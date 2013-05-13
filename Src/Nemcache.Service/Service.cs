using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nemcache.Service.IO;
using Nemcache.Service.Persistence;

namespace Nemcache.Service
{
    class CacheRestHttpHandler : HttpHandlerBase
    {
        private readonly IMemCache _cache;

        public CacheRestHttpHandler(IMemCache cache)
        {
            _cache = cache;
        }

        public override async Task Get(HttpListenerContext httpContext, params string[] matches)
        {
            var key = matches[0];
            var entries = _cache.Retrieve(new[] { key }).
                Select(kve => kve.Value.Data).ToArray();
            if (!entries.Any())
            {
                httpContext.Response.StatusCode = 404;
                httpContext.Response.Close();
            }
            else
            {
                var contentKey = string.Format("content:{0}", key);
                var contentBytes = _cache.Retrieve(new[] { contentKey }).
                    Select(kve => kve.Value.Data).SingleOrDefault();
                string contentType = httpContext.Request.ContentType;
                if (contentBytes != null)
                {
                    //httpContext.Request.ContentType
                    contentType = Encoding.UTF8.GetString(contentBytes);
                }

                httpContext.Response.ContentType = contentType;

                var value = entries.Single();// TODO: does this need converting?
                var outputStream = httpContext.Response.OutputStream;
                await outputStream.WriteAsync(value, 0, value.Length/*, _cancellationTokenSource.Token*/);
                httpContext.Response.Close();
            }
        }

        public override async Task Put(HttpListenerContext context, params string[] matches)
        {
            // TODO: content type...
            var key = matches[0];
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

    internal class Service : IDisposable
    {
        private readonly MemCache _memCache;
        private readonly RequestDispatcher _requestDispatcher;
        private readonly RequestResponseTcpServer _server;
        private readonly CacheRestorer _restorer;
        private StreamArchiver _archiver;
        private readonly FileSystemWrapper _fileSystem;
        private readonly CacheRestServer _restListener;

        public Service(ulong capacity, uint port)
        {
            _memCache = new MemCache(capacity, Scheduler.Default);

            _requestDispatcher = new RequestDispatcher(Scheduler.Default, _memCache);
            _server = new RequestResponseTcpServer(IPAddress.Any, (int) port, _requestDispatcher);
            _restListener = new CacheRestServer(new Dictionary<string, IHttpHandler>
                    {
                        {"/cache/(.+)", new CacheRestHttpHandler(_memCache)},
                        {"/static/(.+)", new StaticFileHttpHandler()}
                    }, new WebSocketHandler(new CancellationTokenSource()));

            _fileSystem = new FileSystemWrapper();
            const string cachelogBin = "cachelog.bin";
            _restorer = new CacheRestorer(_memCache, _fileSystem, cachelogBin);
        }

        public void Start()
        {
            _restorer.RestoreCache();

            _archiver = new StreamArchiver(_fileSystem.File.Open("cachelog.bin", FileMode.OpenOrCreate, FileAccess.Write));

            _memCache.NewNotifications.ObserveOn(Scheduler.Default).Subscribe(_archiver);

            _server.Start();

            _restListener.Start();
            
        }

        public void Stop()
        {
            _server.Stop();
        }

        public void Dispose()
        {
            Stop();
            _server.Dispose();
            _memCache.Dispose();
        }
    }
}