using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;
using Nemcache.Service.RequestHandlers;
using Nemcache.Storage;

namespace Nemcache.Service
{
    internal class Service : IDisposable
    {
        private readonly MemCache _memCache;
        private readonly RequestDispatcher _requestDispatcher;
        private readonly RequestResponseTcpServer _server;
        private readonly ICachePersistence _persistence;
        private readonly IFileSystem _fileSystem;
        private readonly CacheRestServer _restListener;
        private readonly WebSocketServer _websocketServer;

        public Service(
            MemCache memCache,
            RequestDispatcher requestDispatcher,
            RequestResponseTcpServer server,
            CacheRestServer restListener,
            WebSocketServer websocketServer,
            IFileSystem fileSystem,
            ICachePersistence persistence)
        {
            _memCache = memCache;
            _requestDispatcher = requestDispatcher;
            _server = server;
            _restListener = restListener;
            _websocketServer = websocketServer;
            _fileSystem = fileSystem;
            _persistence = persistence;
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
            _persistence.Restore();

            _memCache.Notifications.ObserveOn(Scheduler.Default).Subscribe(_persistence);

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