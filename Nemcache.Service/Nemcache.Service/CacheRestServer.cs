using System;
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
                var webSocketContext = await httpContext.AcceptWebSocketAsync(subProtocol: string.Empty);

                await OnWebSocket(webSocketContext.WebSocket);
            }
            else if (httpContext.Request.HttpMethod == "GET")
            {
                var regex = new Regex("/cache/(.+)");
                var match = regex.Match(rawUrl);
                if (match.Success)
                {
                    var key = match.Groups[1].Value;
                    var entries = _cache.Retrieve(new [] { key }).ToArray();
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
        }

        private async Task OnWebSocket(WebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                await Task.Delay(1000);
                var response = Encoding.ASCII.GetBytes("Hello, World!");
                var arraySegment = new ArraySegment<byte>(response);
                await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, false, _cancellationTokenSource.Token);
            }
        }


        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
        }
    }
}