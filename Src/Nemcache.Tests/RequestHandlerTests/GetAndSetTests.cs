using NUnit.Framework;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestFixture]
    public class GetAndSetTests
    {
        private IClient _client;

        public IClient Client { get; set; }

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        [SetUp]
        public void Setup()
        {
            _client = Client ?? new LocalRequestHandlerWithTestScheduler();
        }


        [Test]
        public void Set()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "value");

            var response = Dispatch(setBuilder.ToAsciiRequest());

            Assert.That(response.ToAsciiString(), Is.EqualTo("STORED\r\n"));
        }

        [Test]
        public void SetNoReply()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "value");
            setBuilder.NoReply();

            var response = Dispatch(setBuilder.ToAsciiRequest());

            Assert.That(response.ToAsciiString(), Is.EqualTo(""));
        }

        [Test]
        public void SetTwice()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            storageBuilder.Data("Updated");
            var response = Dispatch(storageBuilder.ToAsciiRequest());

            Assert.That(response.ToAsciiString(), Is.EqualTo("STORED\r\n"));
        }

        [Test]
        public void SetThenGet()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.That(response.ToAsciiString(), Is.EqualTo("VALUE key 0 5\r\nvalue\r\nEND\r\n"));
        }

        [Test]
        public void FlagsSetAndGet()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            storageBuilder.WithFlags(1234567890);
            Dispatch(storageBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.That(response.ToAsciiString(), Is.EqualTo("VALUE key 1234567890 5\r\nvalue\r\nEND\r\n"));
        }

        [Test]
        public void FlagsMaxValueSetAndGet()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            storageBuilder.WithFlags(ushort.MaxValue);
            Dispatch(storageBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");

            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.That(response.ToAsciiString(), Is.EqualTo("VALUE key " + ushort.MaxValue.ToString() + " 5\r\nvalue\r\nEND\r\n"));
        }

        [Test]
        public void SetAndSetNewThenGet()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());
            storageBuilder.Data("new value");
            Dispatch(storageBuilder.ToAsciiRequest());
            var getBuilder = new GetRequestBuilder("get", "key");

            var response = Dispatch(getBuilder.ToAsciiRequest());

            Assert.That(response.ToAsciiString(), Is.EqualTo("VALUE key 0 9\r\nnew value\r\nEND\r\n"));
        }

        [Test]
        public void SetThenGetMultiple()
        {
            var storageBuilder1 = new StoreRequestBuilder("set", "key1", "111111");
            Dispatch(storageBuilder1.ToAsciiRequest());

            var storageBuilder2 = new StoreRequestBuilder("set", "key2", "222");
            Dispatch(storageBuilder2.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key1", "key2");
            var response = Dispatch(getBuilder.ToAsciiRequest());

            Assert.That(response.ToAsciiString(), Is.EqualTo("VALUE key1 0 6\r\n111111\r\nVALUE key2 0 3\r\n222\r\nEND\r\n"));
        }

        [Test]
        public void GetNotFound()
        {
            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.That(response.ToAsciiString(), Is.EqualTo("END\r\n"));
        }
    }
}