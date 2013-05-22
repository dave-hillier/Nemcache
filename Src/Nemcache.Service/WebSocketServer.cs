using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class WebSocketServer
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly HttpListener _listener = new HttpListener();
        private readonly TaskFactory _taskFactory;
        private readonly IWebSocketHandler _webSocketHandler;

        public WebSocketServer(IWebSocketHandler webSocketHandler, string[] prefixes)
        {
            _taskFactory = new TaskFactory(_cancellationTokenSource.Token);
            foreach (var prefix in prefixes)
            {
                _listener.Prefixes.Add(prefix);
            }
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
            if (httpContext.Request.IsWebSocketRequest) 
            {
                //var rawUrl = httpContext.Request.RawUrl;
                //if (rawUrl == "/cache/notifications")
                {
                    var webSocketContext = await httpContext.AcceptWebSocketAsync(subProtocol: "nemcache-0.1");
                    
                    _webSocketHandler.OnWebSocketConnected(webSocketContext.WebSocket);
                }
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
    }
}