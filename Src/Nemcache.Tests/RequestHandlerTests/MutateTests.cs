using NUnit.Framework;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestFixture]
    public class MutateTests
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
        public void IncrNotFound()
        {
            var builder = new MutateRequestBuilder("incr", "key", 1);
            var response = Dispatch(builder.ToAsciiRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }

        [Test]
        public void DecrNotFound()
        {
            var builder = new MutateRequestBuilder("decr", "key", 1);
            var response = Dispatch(builder.ToAsciiRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }

        [Test]
        public void Incr()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "123");
            Dispatch(storageBuilder.ToAsciiRequest());
            var builder = new MutateRequestBuilder("incr", "key", 1);
            var response = Dispatch(builder.ToAsciiRequest());
            Assert.AreEqual("124\r\n", response.ToAsciiString());
        }

        [Test]
        public void Decr()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "123");
            Dispatch(storageBuilder.ToAsciiRequest());
            var builder = new MutateRequestBuilder("decr", "key", 1);
            var response = Dispatch(builder.ToAsciiRequest());
            Assert.AreEqual("122\r\n", response.ToAsciiString());
        }

        // TODO: incr/decr max and overflow, non-int start value
    }
}