using System.Net;
using System.Reactive.Concurrency;

namespace Nemcache.Service
{
    internal class Service
    {
        private readonly RequestResponseTcpServer _server;
        private readonly MemCache _memCache;
        private readonly RequestDispatcher _requestDispatcher;

        public Service(ulong capacity, uint port)
        {
            _memCache = new MemCache(capacity);

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
    }
}