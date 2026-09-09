using NUnit.Framework;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestFixture]
    public class DeleteTests
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
        public void DeleteNotFound()
        {
            var delBuilder = new DeleteRequestBuilder("key");
            var response = Dispatch(delBuilder.ToAsciiRequest());

            Assert.That(response.ToAsciiString(), Is.EqualTo("NOT_FOUND\r\n"));
        }

        [Test]
        public void DeleteExisting()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var delBuilder = new DeleteRequestBuilder("key");
            var response = Dispatch(delBuilder.ToAsciiRequest());

            Assert.That(response.ToAsciiString(), Is.EqualTo("DELETED\r\n"));
        }

        [Test]
        public void DeleteExistingGetNotFound()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var delBuilder = new DeleteRequestBuilder("key");
            Dispatch(delBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.That(response.ToAsciiString(), Is.EqualTo("END\r\n"));
        }
    }
}