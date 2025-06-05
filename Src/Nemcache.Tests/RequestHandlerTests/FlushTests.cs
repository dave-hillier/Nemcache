using System;
using System.Text;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestFixture]
    public class FlushTests
    {
        private IClient _client;
        private TestScheduler _testScheduler;

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        [SetUp]
        public void Setup()
        {
            var client = new LocalRequestHandlerWithTestScheduler();
            _client = client;
            _testScheduler = client.TestScheduler;
        }

        [Test]
        public void FlushResponse()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all\r\n");
            var response = Dispatch(flushRequest);

            Assert.AreEqual("OK\r\n", response.ToAsciiString());
        }

        [Test]
        public void FlushDelayResponse()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all 123\r\n");
            var response = Dispatch(flushRequest);

            Assert.AreEqual("OK\r\n", response.ToAsciiString());
        }

        [Test]
        public void FlushClearsCache()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all\r\n");
            Dispatch(flushRequest);

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        [Test]
        public void FlushClearsCacheMultiple()
        {
            var storageBuilder1 = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder1.ToAsciiRequest());
            var storageBuilder2 = new StoreRequestBuilder("set", "key2", "value");
            Dispatch(storageBuilder2.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all\r\n");
            Dispatch(flushRequest);

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        [Test]
        public void FlushWithDelayNoEffect()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all 100\r\n");
            Dispatch(flushRequest);

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(90).Ticks);

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }


        [Test]
        public void FlushWithDelayEmpty()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all 100\r\n");
            Dispatch(flushRequest);

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(200).Ticks);

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
    }
}