using System;
using System.IO;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nemcache.Service.IO;
using Nemcache.Service.Notifications;
using Nemcache.Service.Reactive;

namespace Nemcache.Service
{
    internal class Service
    {
        private readonly RequestResponseTcpServer _server;
        private readonly MemCache _memCache;

        public Service(ulong capacity, uint port)
        {
            _memCache = new MemCache(capacity);

            var requestHandler = new RequestHandler(Scheduler.Default, _memCache);
            _server = new RequestResponseTcpServer(IPAddress.Any, (int) port, requestHandler.Dispatch);
            

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