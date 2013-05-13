using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using Nemcache.Service;
using Nemcache.Service.RequestHandlers;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    public class LocalRequestHandlerWithTestScheduler : IClient
    {
        private readonly MemCache _cache;
        private readonly RequestDispatcher _requestDispatcher;
        private readonly TestScheduler _scheduler;

        public LocalRequestHandlerWithTestScheduler()
        {
            _scheduler = new TestScheduler();
            _cache = new MemCache(capacity: 100);
            var requestHandlers = Service.Service.GetRequestHandlers(_scheduler, _cache);
            _requestDispatcher = new RequestDispatcher(_scheduler, _cache, requestHandlers);
        }

        public TestScheduler TestScheduler
        {
            get { return _scheduler; }
        }

        public byte[] Send(byte[] p)
        {
            var memoryStream = new MemoryStream(1024);
            _requestDispatcher.Dispatch(new MemoryStream(p), memoryStream, "", OnDisconnect).Wait();
            return memoryStream.ToArray();
        }

        public Action OnDisconnect { get; set; }

        public void Capacity(int i)
        {
            _cache.Capacity = 10;
        }
    }

}