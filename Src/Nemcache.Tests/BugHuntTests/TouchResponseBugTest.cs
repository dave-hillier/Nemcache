using System;
using System.IO;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Nemcache.Client.Builders;
using Nemcache.Storage;
using Nemcache.Service.RequestHandlers;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    /// <summary>
    /// Bug: TouchHandler returns "OK\r\n" on success instead of "TOUCHED\r\n".
    /// Per memcached protocol, the touch command should return "TOUCHED\r\n".
    /// See TouchHandler.cs line 30.
    /// </summary>
    [TestFixture]
    public class TouchResponseBugTest
    {
        private RequestDispatcher _requestDispatcher;
        private TestScheduler _testScheduler;

        private byte[] Dispatch(byte[] p)
        {
            var memoryStream = new MemoryStream(1024);
            _requestDispatcher.Dispatch(new MemoryStream(p), memoryStream, "", null).Wait();
            return memoryStream.ToArray();
        }

        [SetUp]
        public void Setup()
        {
            _testScheduler = new TestScheduler();
            IMemCache cache = new MemCache(capacity: 100);
            _requestDispatcher = new RequestDispatcher(
                _testScheduler, cache,
                Service.Service.GetRequestHandlers(_testScheduler, cache));
        }

        [Test]
        public void TouchShouldReturnTouchedNotOk()
        {
            // Set up an entry first
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            storageBuilder.WithExpiry(100);
            Dispatch(storageBuilder.ToAsciiRequest());

            // Touch it
            var touchBuilder = new TouchRequestBuilder("key");
            touchBuilder.WithExpiry(200);
            var response = Dispatch(touchBuilder.ToAsciiRequest());

            // Per memcached protocol, successful touch should return "TOUCHED\r\n"
            Assert.AreEqual("TOUCHED\r\n", response.ToAsciiString(),
                "Memcached protocol specifies 'TOUCHED\\r\\n' response, not 'OK\\r\\n'");
        }
    }
}
