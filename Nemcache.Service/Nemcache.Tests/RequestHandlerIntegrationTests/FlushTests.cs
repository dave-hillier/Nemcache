using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Client.Builders;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class FlushTests
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

        #region flush

        [TestMethod]
        public void FlushResponse()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all\r\n");
            var response = _requestHandler.Dispatch("remote", flushRequest, null);

            Assert.AreEqual("OK\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void FlushDelayResponse()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all 123\r\n");
            var response = _requestHandler.Dispatch("remote", flushRequest, null);

            Assert.AreEqual("OK\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void FlushClearsCache()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all\r\n");
            _requestHandler.Dispatch("remote", flushRequest, null);

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void FlushClearsCacheMultiple()
        {
            var storageBuilder1 = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder1.ToAsciiRequest());
            var storageBuilder2 = new StoreRequestBuilder("set", "key2", "value");
            Dispatch(storageBuilder2.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all\r\n");
            _requestHandler.Dispatch("remote", flushRequest, null);

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void FlushWithDelayNoEffect()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all 100\r\n");
            _requestHandler.Dispatch("remote", flushRequest, null);

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(90));

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }


        [TestMethod]
        public void FlushWithDelayEmpty()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToAsciiRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all 100\r\n");
            _requestHandler.Dispatch("remote", flushRequest, null);

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(200));

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        #endregion
    }
}