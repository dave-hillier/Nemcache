using System.Collections.Generic;
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

        public CacheRestServer(Dictionary<string, IHttpHandler> httpHandlers, string[] prefixes)
        {
            _taskFactory = new TaskFactory(_cancellationTokenSource.Token);
            foreach (var prefix in prefixes)
            {
                _listener.Prefixes.Add(prefix);
            }
            _httpHandlers = httpHandlers;
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
            var handlerfound = (from kv in _httpHandlers
                                let regex = new Regex(kv.Key)
                                let match = regex.Match(rawUrl)
                                where match.Success
                                select new { Match = match, Handler = kv.Value }).FirstOrDefault();
            if (handlerfound != null)
            {
                string[] value = new string[]{};

                if (handlerfound.Match.Groups.Count > 1)
                {
                    value = handlerfound.Match.Groups.
                        OfType<Group>().Skip(1).Select(g => g.Value).ToArray();
                }
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

       
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
        }
    }
}