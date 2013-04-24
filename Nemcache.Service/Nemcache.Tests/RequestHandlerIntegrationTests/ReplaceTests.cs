using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nemcache.Service;
using System.Threading;
using Nemcache.Client.Builders;

namespace Nemcache.Tests
{
    [TestClass]
    class ReplaceTests
    {
        RequestHandler _requestHandler;
        TestScheduler _testScheduler;

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


        #region Replace

        [TestMethod]
        public void ReplaceToEmpty()
        {
            var replaceBuilder = new StoreRequestBuilder("replace", "key", "value");

            var response = Dispatch(replaceBuilder.ToAsciiRequest());

            Assert.AreEqual("NOT_STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void ReplaceNoReply()
        {
            var replaceBuilder = new StoreRequestBuilder("replace", "key", "value");
            replaceBuilder.NoReply();

            var response = Dispatch(replaceBuilder.ToAsciiRequest());

            Assert.AreEqual("", response.ToAsciiString());
        }

        [TestMethod]
        public void ReplaceToExisting()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(setBuilder.ToAsciiRequest());

            var replaceBuilder = new StoreRequestBuilder("replace", "key", "value");
            var response = Dispatch(replaceBuilder.ToAsciiRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        // TODO: replace ignores existing flags and exp

        [TestMethod]
        public void GetValueOfReplaceToExisting()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "first");
            Dispatch(setBuilder.ToAsciiRequest());

            var replaceBuilder = new StoreRequestBuilder("replace", "key", "second");
            Dispatch(replaceBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 6\r\nsecond\r\nEND\r\n", response.ToAsciiString());
        }
        #endregion

    }
}
