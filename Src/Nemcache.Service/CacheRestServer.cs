using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class CacheRestServer
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Dictionary<string, IHttpHandler> _handlers;
        private readonly HttpListener _listener = new HttpListener();
        private readonly TaskFactory _taskFactory;

        public CacheRestServer(Dictionary<string, IHttpHandler> handlers)
        {
            _taskFactory = new TaskFactory(_cancellationTokenSource.Token);
            _listener.Prefixes.Add("http://localhost:8222/");
            _handlers = handlers;
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
            var rawUrl = httpContext.Request.RawUrl;
            if (httpContext.Request.IsWebSocketRequest)
            {
                if (rawUrl == "/cache/notifications")
                {
                    // Todo json, text or binary?
                    var webSocketContext = await httpContext.AcceptWebSocketAsync(subProtocol: "nemcache-0.1");
                    OnWebSocket(webSocketContext.WebSocket);
                }
            }
            else
            {
                var handlerfound = (from kv in _handlers
                                    let regex = new Regex(kv.Key)
                                    let match = regex.Match(rawUrl)
                                    where match.Success
                                    select new { Match = match, Handler = kv.Value }).FirstOrDefault();
                if (handlerfound != null)
                {
                    // TODO: pass the rest of the matches to the handler
                    var value = handlerfound.Match.Groups[1].Value;
                    switch (httpContext.Request.HttpMethod)
                    {
                        case "GET":
                            await handlerfound.Handler.Get(httpContext, value);
                            break;
                        case "PUT":
                            await handlerfound.Handler.Put(httpContext, value);
                            break;
                        case "DELETE":
                            await handlerfound.Handler.Delete(httpContext, value);
                            break;
                        case "POST":
                            await handlerfound.Handler.Post(httpContext, value);
                            break;
                    }
                }
                else
                {
                    httpContext.Response.StatusCode = 404;
                    httpContext.Response.Close();
                }
            }
        }

        private void OnWebSocket(WebSocket webSocket)
        {
            Task.WaitAll(
                SendLoop(webSocket), 
                ReceiveLoop(webSocket));
        }

        private async Task ReceiveLoop(WebSocket webSocket)
        {
            var receiveBuffer = new byte[4096];
            while (webSocket.State == WebSocketState.Open)
            {
                var arraySegment = new ArraySegment<byte>(receiveBuffer);
                var receiveResult = await webSocket.ReceiveAsync(arraySegment, _cancellationTokenSource.Token);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", _cancellationTokenSource.Token);
                }
                else if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    Console.WriteLine("Received: {0}", Encoding.ASCII.GetString(arraySegment.Array).Trim('\0'));
                }
            }
        }

        private async Task SendLoop(WebSocket webSocket)
        {
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

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
        }
    }
}