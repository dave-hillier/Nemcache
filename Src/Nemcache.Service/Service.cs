using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nemcache.Service.IO;
using Nemcache.Service.Persistence;
using Nemcache.Service.RequestHandlers;

namespace Nemcache.Service
{
    public class Service : IDisposable
    {
        private readonly MemCache _memCache;
        private readonly RequestDispatcher _requestDispatcher;
        private readonly RequestResponseTcpServer _server;
        private readonly CacheRestorer _restorer;
        private StreamArchiver _archiver;
        private readonly FileSystemWrapper _fileSystem;
        private readonly CacheRestServer _restListener;
        private readonly WebSocketServer _websocketServer;

        public Service(ulong capacity, uint port)
        {
            _memCache = new MemCache(capacity, Scheduler.Default);

            IScheduler scheduler = Scheduler.Default;

            var requestHandlers = GetRequestHandlers(scheduler, _memCache);
            _requestDispatcher = new RequestDispatcher(scheduler, _memCache, requestHandlers);
            _server = new RequestResponseTcpServer(IPAddress.Any, (int)port, _requestDispatcher);
            _restListener = new CacheRestServer(new Dictionary<string, IHttpHandler>
                    {
                        {"/cache/(.+)", new CacheRestHttpHandler(_memCache)},
                        {"/static/(.+)", new StaticFileHttpHandler()}
                    }, 
                    new[]
                        {
                            "http://localhost:8222/cache/",
                            "http://localhost:8222/static/"
                        });
            _websocketServer = new WebSocketServer(new [] { "http://localhost:8222/sub/" },
                o => new CacheEntrySubscriptionHandler(_memCache, o) );
            _fileSystem = new FileSystemWrapper();
            const string cachelogBin = "cachelog.bin";
            _restorer = new CacheRestorer(_memCache, _fileSystem, cachelogBin);
        }

        public static Dictionary<string, IRequestHandler> GetRequestHandlers(IScheduler scheduler, IMemCache cache)
        {
            var helpers = new RequestConverters(scheduler);
            var getHandler = new GetHandler(helpers, cache, scheduler);
            var mutateHandler = new MutateHandler(helpers, cache, scheduler);
            return new Dictionary<string, IRequestHandler>
                {
                    {"get", getHandler}, {"gets", getHandler}, {"set", new SetHandler(helpers, cache)}, {"append", new AppendHandler(helpers, cache)}, {"prepend", new PrependHandler(helpers, cache)}, {"add", new AddHandler(helpers, cache)}, {"replace", new ReplaceHandler(helpers, cache)}, {"cas", new CasHandler(helpers, cache)}, {"stats", new StatsHandler()}, {"delete", new DeleteHandler(helpers, cache)}, {"flush_all", new FlushHandler(cache, scheduler)}, {"quit", new QuitHandler()}, {"exception", new ExceptionHandler()}, {"version", new VersionHandler()}, {"touch", new TouchHandler(helpers, cache)}, {"incr", mutateHandler}, {"decr", mutateHandler},
                };

        }
        public void Start()
        {
            _restorer.RestoreCache();

            _archiver = new StreamArchiver(_fileSystem,
                "cachelog.bin",
                _memCache,
                10000);

            _memCache.Notifications.ObserveOn(Scheduler.Default).Subscribe(_archiver);

            _server.Start();

            _restListener.Start();
            _websocketServer.Start();
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