using System;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Client.Builders;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class ExpiryTests
    {
        private IClient _client;
        private TestScheduler _testScheduler;

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        public IClient Client { get; set; }

        [TestInitialize]
        public void Setup()
        {
            var client = new LocalRequestHandlerWithTestScheduler();
            _client = Client ?? client;
            _testScheduler = client.TestScheduler;
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

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(2).Ticks);

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void AbsoluteLongExpiry()
        {
            var unixEpoc = new DateTime(1970, 1, 1);
            _testScheduler.AdvanceBy((unixEpoc - _testScheduler.Now).Ticks); // Advance to the start of unix time
            TimeSpan span = (new DateTime(1970, 6, 1) - unixEpoc);
            var unixTime = (int) span.TotalSeconds;

            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            storageBuilder.WithExpiry(unixTime);

            Dispatch(storageBuilder.ToAsciiRequest());

            _testScheduler.AdvanceBy(TimeSpan.FromDays(200).Ticks);

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
    }
}