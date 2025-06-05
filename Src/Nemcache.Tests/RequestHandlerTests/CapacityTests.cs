using NUnit.Framework;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestFixture]
    public class CapacityTests
    {
        private IClient _client;

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        [SetUp]
        public void Setup()
        {
            var client = new LocalRequestHandlerWithTestScheduler();
            client.Capacity(10);
            _client = client;
        }

        [Test]
        public void StoreInCapacity()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "1234567890");

            var response = Dispatch(setBuilder.ToAsciiRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [Test]
        public void StoreCasInCapacity()
        {
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

        [Test]
        public void StoreOverCapacity()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "12345678901");

            var response = Dispatch(setBuilder.ToAsciiRequest());

            Assert.AreEqual("ERROR Over capacity\r\n", response.ToAsciiString());
        }


        [Test]
        public void StoreEvictOverCapacity()
        {
            var setBuilder1 = new StoreRequestBuilder("set", "key1", "1234567890");
            var setBuilder2 = new StoreRequestBuilder("set", "key2", "1234567890");

            Dispatch(setBuilder1.ToAsciiRequest());
            var response = Dispatch(setBuilder2.ToAsciiRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());

            var getBuilder = new GetRequestBuilder("get", "key1");
            var response2 = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response2.ToAsciiString());
        }

        [Test]
        public void StoreMultipleEvictOverCapacity()
        {
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