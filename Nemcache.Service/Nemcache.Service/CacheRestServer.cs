using System;
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

        private readonly IMemCache _cache;
        private readonly HttpListener _listener = new HttpListener();
        private readonly TaskFactory _taskFactory;

        public CacheRestServer(IMemCache cache)
        {
            _taskFactory = new TaskFactory(_cancellationTokenSource.Token);
            _listener.Prefixes.Add("http://localhost:8222/cache/");
            _cache = cache;
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
                    //webSocketContext.
                    await OnWebSocket(webSocketContext.WebSocket);
                }

            }
            else if (httpContext.Request.HttpMethod == "GET")
            {
                if (rawUrl == "/cache/test")
                {
                    var bytes = File.ReadAllBytes("test.html");
                    httpContext.Response.ContentType = "text/html";
                    httpContext.Response.StatusCode = 200;
                    await httpContext.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    httpContext.Response.Close();
                }
                else
                    await HandleGet(httpContext, rawUrl);
            }
            else if (httpContext.Request.HttpMethod == "PUT")
            {
                await HandlePut(httpContext, rawUrl);
            }
            // Todo: put/post/delete
        }

        private async Task HandlePut(HttpListenerContext httpContext, string rawUrl)
        {
        }

        private async Task HandleGet(HttpListenerContext httpContext, string rawUrl)
        {
            var regex = new Regex("/cache/(.+)");
            var match = regex.Match(rawUrl);
            if (match.Success)
            {
                var key = match.Groups[1].Value;
                var entries = _cache.Retrieve(new[] {key}).ToArray();
                if (!entries.Any())
                {
                    httpContext.Response.StatusCode = 404;
                    httpContext.Response.Close();
                }
                else
                {
                    // TODO: retrieve mime-type -- perhaps reserved keys
                    var value = entries.Single().Value.Data;
                    var outputStream = httpContext.Response.OutputStream;
                    await outputStream.WriteAsync(value, 0, value.Length, _cancellationTokenSource.Token);
                    httpContext.Response.Close();
                }
            }
        }

        private async Task OnWebSocket(WebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var response = Encoding.ASCII.GetBytes("Ping!");
                var arraySegment = new ArraySegment<byte>(response);
                await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, _cancellationTokenSource.Token);

                await Task.Delay(1000);
                /*var receiveBuffer = new byte[4096];
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                else if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    while (true)
                    {
                        
                        var response = Encoding.ASCII.GetBytes("Ping!");
                        var arraySegment = new ArraySegment<byte>(response);
                        await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
                        
                        await Task.Delay(1000);
                    }
                    //await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept text frame", CancellationToken.None);
                }
                else
                {
                    //await webSocket.SendAsync(new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count), WebSocketMessageType.Binary, receiveResult.EndOfMessage, CancellationToken.None);
                }*/
            }
        }


        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
        }
    }
}