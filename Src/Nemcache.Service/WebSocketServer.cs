using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class WebSocketServer
    {
        private readonly WebSocketSubscriptionHandler _handler;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly HttpListener _listener = new HttpListener();
        private readonly TaskFactory _taskFactory;

        public WebSocketServer(IEnumerable<string> prefixes, WebSocketSubscriptionHandler handler)
        {
            _handler = handler;
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
            var blockingCollection = new BlockingCollection<string>();
            var subject = new Subject<string>();
            using (subject.Synchronize().Subscribe(n => blockingCollection.Add(n)))
            {
                Task.WaitAll(
                    ReceiveLoop(webSocket, subject),
                    SendLoop(webSocket, blockingCollection)
                    );
            }
        }

        private async Task ReceiveLoop(WebSocket webSocket, IObserver<string> observer)
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
                        // TODO: receive entire message
                        //OnNext the command, let the subscription manager handle it, provide the current context/reponse action?

                        var message = Encoding.UTF8.GetString(arraySegment.Array);
                        _handler.HandleMessage(message, observer);
                        break;
                }
            }
            // OnComplete here??
        }

        private async Task SendLoop(WebSocket webSocket, BlockingCollection<string> sendQueue)
        {
            // TODO: what do identify a client on?
            // TODO: create a client message queue

            foreach (var value in sendQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                if (webSocket.State != WebSocketState.Open)
                    break;
                var arraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(value));
                await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            }
        }
    }
}