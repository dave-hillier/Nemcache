using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class AddTest
    {
        private IClient _client;

        public IClient Client { get; set; }

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        [TestInitialize]
        public void Setup()
        {
            _client = Client ?? new LocalRequestHandlerWithTestScheduler();
        }

        #region Add

        [TestMethod]
        public void AddToEmpty()
        {
            var addBuilder = new StoreRequestBuilder("add", "key", "value");

            var response = Dispatch(addBuilder.ToAsciiRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void AddNoReply()
        {
            var addBuilder = new StoreRequestBuilder("add", "key", "value");
            addBuilder.NoReply();

            var response = Dispatch(addBuilder.ToAsciiRequest());

            Assert.AreEqual("", response.ToAsciiString());
        }

        [TestMethod]
        public void AddToExisting()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "value");

            var response2 = Dispatch(setBuilder.ToAsciiRequest()).ToAsciiString();

            var addBuilder = new StoreRequestBuilder("add", "key", "value");

            var response = Dispatch(addBuilder.ToAsciiRequest());

            Assert.AreEqual("NOT_STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void GetValueOfAddToEmpty()
        {
            var addBuilder = new StoreRequestBuilder("add", "key", "value");

            Dispatch(addBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        #endregion
    }
}