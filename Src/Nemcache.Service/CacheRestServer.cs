using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class CacheRestServer
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Dictionary<string, IHttpHandler> _httpHandlers;
        private readonly HttpListener _listener = new HttpListener();
        private readonly TaskFactory _taskFactory;
        private readonly IWebSocketHandler _webSocketHandler;

        public CacheRestServer(Dictionary<string, IHttpHandler> httpHandlers, IWebSocketHandler webSocketHandler)
        {
            _taskFactory = new TaskFactory(_cancellationTokenSource.Token);
            _listener.Prefixes.Add("http://localhost:8222/");
            _httpHandlers = httpHandlers;
            _webSocketHandler = webSocketHandler;
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
                    var webSocketContext = await httpContext.AcceptWebSocketAsync(subProtocol: "nemcache-0.1");
                    _webSocketHandler.OnWebSocketConnected(webSocketContext.WebSocket);
                }
            }
            else
            {
                var handlerfound = (from kv in _httpHandlers
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

       
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
        }
    }
}