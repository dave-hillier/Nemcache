using System;
using System.Net;
using System.Reactive.Concurrency;

namespace Nemcache.Service
{
    internal class Service : IDisposable
    {
        private readonly MemCache _memCache;
        private readonly RequestDispatcher _requestDispatcher;
        private readonly RequestResponseTcpServer _server;

        public Service(ulong capacity, uint port)
        {
            _memCache = new MemCache(capacity, Scheduler.Default);
            _requestDispatcher = new RequestDispatcher(Scheduler.Default, _memCache);
            _server = new RequestResponseTcpServer(IPAddress.Any, (int) port, _requestDispatcher);
        }

        public void Start()
        {
            _server.Start();
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