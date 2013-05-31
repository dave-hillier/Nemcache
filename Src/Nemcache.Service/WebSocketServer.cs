using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private readonly Func<IObserver<string>, WebSocketSubscriptionHandler> _handlerFactory;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly HttpListener _listener = new HttpListener();
        private readonly TaskFactory _taskFactory;

        public WebSocketServer(IEnumerable<string> prefixes, Func<IObserver<String>, WebSocketSubscriptionHandler> handlerFactory)
        {
            _handlerFactory = handlerFactory;
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
            while (!_cancellationTokenSource.IsCancellationRequested && _listener.IsListening)
            {
                var httpContext = await _listener.GetContextAsync();
                _taskFactory.StartNew(() => OnClientConnection(httpContext));
            }
        }

        private async Task OnClientConnection(HttpListenerContext httpContext)
        {
            if (httpContext.Request.IsWebSocketRequest)
            {
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
            using (subject.Synchronize().Subscribe(blockingCollection.Add))
            {
                using (var webSocketSubscriptionHandler = _handlerFactory(subject))
                {
                    Task.WaitAll(
                        ReceiveLoop(webSocket, webSocketSubscriptionHandler),
                        SendLoop(webSocket, blockingCollection)
                        );
                }
            }
        }

        private async Task ReceiveLoop(WebSocket webSocket, WebSocketSubscriptionHandler handler)
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
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                                                   "", _cancellationTokenSource.Token);
                        break;
                    case WebSocketMessageType.Text:
                        var message = await OnMessage(webSocket, arraySegment, receiveResult);
                        handler.HandleMessage(message);
                        break;
                }
            }
        }

        private async Task<string> OnMessage(WebSocket webSocket, ArraySegment<byte> arraySegment, WebSocketReceiveResult receiveResult)
        {
            var stream = new MemoryStream();
            stream.Write(arraySegment.Array, 0, receiveResult.Count);
            var message = Encoding.UTF8.GetString(stream.ToArray());
            
            if (receiveResult.EndOfMessage)
            {
                return message;
            }
 
            var originalType = receiveResult.MessageType;
            while (true)
            {
                receiveResult = await webSocket.ReceiveAsync(arraySegment, _cancellationTokenSource.Token);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (originalType != receiveResult.MessageType)
                {
                    throw new InvalidOperationException();
                }

                stream.Write(arraySegment.Array, 0, receiveResult.Count);
                message = Encoding.UTF8.GetString(stream.ToArray());
                
                if (receiveResult.EndOfMessage)
                {
                    return message;
                }
            }
            return "";
        }

        private async Task SendLoop(WebSocket webSocket, BlockingCollection<string> sendQueue)
        {
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