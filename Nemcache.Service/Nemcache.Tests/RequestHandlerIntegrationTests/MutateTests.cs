using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Client.Builders;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class MutateTests
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
        public void IncrNotFound()
        {
            var builder = new MutateRequestBuilder("incr", "key", 1);
            var response = Dispatch(builder.ToAsciiRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void DecrNotFound()
        {
            var builder = new MutateRequestBuilder("decr", "key", 1);
            var response = Dispatch(builder.ToAsciiRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void Incr()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "123");
            Dispatch(storageBuilder.ToAsciiRequest());
            var builder = new MutateRequestBuilder("incr", "key", 1);
            var response = Dispatch(builder.ToAsciiRequest());
            Assert.AreEqual("124\r\n", response.ToAsciiString());
        }

        [TestMethod]
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