using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Client.Builders;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class CapacityTests
    {
        private RequestHandler _requestHandler;

        [TestInitialize]
        public void Setup()
        {
            _requestHandler = new RequestHandler(new TestScheduler(), new MemCache(capacity:10));
        }


        private byte[] Dispatch(byte[] p)
        {
            return _requestHandler.Dispatch("", p, null);
        }

        [TestMethod]
        public void StoreInCapacity()
        {
            _requestHandler = new RequestHandler(new TestScheduler(), new MemCache(capacity:10));
            var setBuilder = new StoreRequestBuilder("set", "key", "1234567890");

            var response = Dispatch(setBuilder.ToAsciiRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void StoreCasInCapacity()
        {
            _requestHandler = new RequestHandler(new TestScheduler(), new MemCache(capacity:10));
            var setBuilder = new StoreRequestBuilder("set", "s", "12345");
            Dispatch(setBuilder.ToAsciiRequest());

            var builder = new CasRequestBuilder("key", "12345");
            builder.WithCasUnique(123);
            Dispatch(builder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            Dispatch(getBuilder.ToAsciiRequest());

            var builder2 = new CasRequestBuilder("key", "123456");
            builder2.WithCasUnique(123);
            var response2 = Dispatch(builder2.ToAsciiRequest());

            Assert.AreEqual("STORED\r\n", response2.ToAsciiString());
        }

        [TestMethod]
        public void StoreOverCapacity()
        {
            _requestHandler = new RequestHandler(new TestScheduler(), new MemCache(capacity:1));
            var setBuilder = new StoreRequestBuilder("set", "key", "1234567890");

            var response = Dispatch(setBuilder.ToAsciiRequest());

            Assert.AreEqual("ERROR Over capacity\r\n", response.ToAsciiString());
        }


        [TestMethod]
        public void StoreEvictOverCapacity()
        {
            _requestHandler = new RequestHandler(new TestScheduler(), new MemCache(capacity:10));
            var setBuilder1 = new StoreRequestBuilder("set", "key1", "1234567890");
            var setBuilder2 = new StoreRequestBuilder("set", "key2", "1234567890");

            Dispatch(setBuilder1.ToAsciiRequest());
            var response = Dispatch(setBuilder2.ToAsciiRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());

            var getBuilder = new GetRequestBuilder("get", "key1");
            var response2 = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response2.ToAsciiString());
        }

        [TestMethod]
        public void StoreMultipleEvictOverCapacity()
        {
            _requestHandler = new RequestHandler(new TestScheduler(), new MemCache(capacity:10));
            var setBuilder1 = new StoreRequestBuilder("set", "key1", "12345");
            var setBuilder2 = new StoreRequestBuilder("set", "key2", "12345");
            var setBuilder3 = new StoreRequestBuilder("set", "key3", "1234567890");

            Dispatch(setBuilder1.ToAsciiRequest());
            Dispatch(setBuilder2.ToAsciiRequest());
            Dispatch(setBuilder3.ToAsciiRequest());

            var getBuilder1 = new GetRequestBuilder("get", "key1");
            var response1 = Dispatch(getBuilder1.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response1.ToAsciiString());

            var getBuilder2 = new GetRequestBuilder("get", "key2");
            var response2 = Dispatch(getBuilder2.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response2.ToAsciiString());
        }
    }
}