using System;
using Microsoft.Reactive.Testing;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    public class LocalRequestHandlerWithTestScheduler : IClient
    {
        private readonly TestScheduler _scheduler;
        private readonly MemCache _cache;
        private readonly RequestHandler _requestHandler;

        public LocalRequestHandlerWithTestScheduler()
        {
            _scheduler = new TestScheduler();
            _cache = new MemCache(capacity: 100);
            _requestHandler = new RequestHandler(_scheduler, _cache);
        }

        public byte[] Send(byte[] p)
        {
            return _requestHandler.Dispatch("", p, OnDisconnect);
        }

        public IDisposable OnDisconnect { get; set; }

        public void Capacity(int i)
        {
            _cache.Capacity = 10;
        }

        public TestScheduler TestScheduler { get { return _scheduler;  } }
    }
}