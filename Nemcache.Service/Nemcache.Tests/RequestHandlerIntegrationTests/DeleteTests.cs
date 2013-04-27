using System.Reactive.Concurrency;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Client.Builders;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class DeleteTests
    {
        private RequestHandler _requestHandler;

        private byte[] Dispatch(byte[] p)
        {
            return _requestHandler.Dispatch("", p, null);
        }

        [TestInitialize]
        public void Setup()
        {
            _requestHandler = new RequestHandler(Scheduler.Default, new MemCache(capacity:100));
        }

        #region delete

        [TestMethod]
        public void DeleteNotFound()
        {
            var delBuilder = new DeleteRequestBuilder("key");
            var response = Dispatch(delBuilder.ToAsciiRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void DeleteExisting()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var delBuilder = new DeleteRequestBuilder("key");
            var response = Dispatch(delBuilder.ToAsciiRequest());

            Assert.AreEqual("DELETED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void DeleteExistingGetNotFound()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var delBuilder = new DeleteRequestBuilder("key");
            Dispatch(delBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        #endregion
    }
}