using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Client.Builders;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class ExpiryTests
    {
        private RequestHandler _requestHandler;
        private TestScheduler _testScheduler;

        private byte[] Dispatch(byte[] p)
        {
            return _requestHandler.Dispatch("", p, null);
        }

        [TestInitialize]
        public void Setup()
        {
            _requestHandler = new RequestHandler(100000);
            Scheduler.Current = _testScheduler = new TestScheduler();
        }

        [TestMethod]
        public void SetExpiryThenGet()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            storageBuilder.WithExpiry(100);

            Dispatch(storageBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void SetExpiryThenGetGone()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            storageBuilder.WithExpiry(1);

            Dispatch(storageBuilder.ToAsciiRequest());

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(2));

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void DayLongExpiry()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");

            TimeSpan span = (new DateTime(1970, 6, 1) - new DateTime(1970, 1, 1));
            int unixTime = (int) span.TotalSeconds;

            storageBuilder.WithExpiry(unixTime);

            Dispatch(storageBuilder.ToAsciiRequest());

            _testScheduler.AdvanceBy(TimeSpan.FromDays(200));

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
    }
}