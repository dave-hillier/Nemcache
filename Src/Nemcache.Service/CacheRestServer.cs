using System;
using System.Collections.Concurrent;
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
                    OnWebSocket(webSocketContext.WebSocket);
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
        }

        private async Task HandlePut(HttpListenerContext context, string rawUrl)
        {
            var regex = new Regex("/cache/(.+)");
            var match = regex.Match(rawUrl);
            if (match.Success)
            {
                // TODO: content type...
                var key = match.Groups[1].Value;
                var streamReader = new StreamReader(context.Request.InputStream);
                var body = await streamReader.ReadToEndAsync();

                _cache.Store(key, 0, Encoding.UTF8.GetBytes(body), DateTime.MaxValue);

                byte[] response = Encoding.UTF8.GetBytes("STORED\r\n");
                context.Response.StatusCode = 200;
                context.Response.KeepAlive = false;
                context.Response.ContentLength64 = response.Length;

                var output = context.Response.OutputStream;
                await output.WriteAsync(response, 0, response.Length);
                context.Response.Close();
            }
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
                };// Todo: save this so it is acecssible from somewhere else somehow...
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