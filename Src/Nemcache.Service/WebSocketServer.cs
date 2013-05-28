using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class WebSocketServer
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly HttpListener _listener = new HttpListener();
        private readonly TaskFactory _taskFactory;

        public WebSocketServer(string[] prefixes)
        {
            _taskFactory = new TaskFactory(_cancellationTokenSource.Token);
            foreach (var prefix in prefixes)
            {
                _listener.Prefixes.Add(prefix);
            }
        }

        public void Start()
        {
            _listener.Start();
            _taskFactory.StartNew(ListenForClients);
        }

        private async void ListenForClients()
        {
            while (!_cancellationTokenSource.IsCancellationRequested &&
                   _listener.IsListening)
            {
                var httpContext = await _listener.GetContextAsync();
                await _taskFactory.StartNew(() => OnClientConnection(httpContext));
            }
        }

        private async Task OnClientConnection(HttpListenerContext httpContext)
        {
            if (httpContext.Request.IsWebSocketRequest) 
            {
                //var rawUrl = httpContext.Request.RawUrl
                var webSocketContext = await httpContext.AcceptWebSocketAsync(subProtocol: "nemcache-0.1");
                OnWebSocketConnected(webSocketContext.WebSocket);
            }
            else
            {
                httpContext.Response.StatusCode = 404;
                httpContext.Response.Close();
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
        }

        public void OnWebSocketConnected(WebSocket webSocket)
        {
            Task.WaitAll(SendLoop(webSocket), ReceiveLoop(webSocket));
        }

        private async Task ReceiveLoop(WebSocket webSocket)
        {
            var receiveBuffer = new byte[4096];
            while (webSocket.State == WebSocketState.Open)
            {
                var arraySegment = new ArraySegment<byte>(receiveBuffer);
                var receiveResult = await webSocket.ReceiveAsync(arraySegment, _cancellationTokenSource.Token);
                switch (receiveResult.MessageType)
                {
                    case WebSocketMessageType.Binary:
                        break;
                    case WebSocketMessageType.Close:
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", _cancellationTokenSource.Token);
                        // OnNext a close
                        break;
                    case WebSocketMessageType.Text:
                        //OnNext the command, let the subscription manager handle it, provide the current context/reponse action?
                        break;
                }
            }
            // OnComplete here??
        }

        private async Task SendLoop(WebSocket webSocket)
        {
            // TODO: what do identify a client on?
            // TODO: create a client message queue
            var sendQueue = new BlockingCollection<byte[]>
                {
                    Encoding.UTF8.GetBytes("hello"),
                    Encoding.UTF8.GetBytes("world"),
                    Encoding.UTF8.GetBytes("foo"),
                    Encoding.UTF8.GetBytes("bar")
                };// TODO: save this so it is acecssible from somewhere else somehow...

            foreach (var val in sendQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                if (webSocket.State != WebSocketState.Open)
                    break;
                var arraySegment = new ArraySegment<byte>(val);
                await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            }
        }
    }
}