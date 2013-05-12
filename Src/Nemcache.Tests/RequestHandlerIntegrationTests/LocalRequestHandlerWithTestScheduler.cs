using System;
using System.IO;
using Microsoft.Reactive.Testing;
using Nemcache.Service;

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
            _requestDispatcher = new RequestDispatcher(_scheduler, _cache);
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