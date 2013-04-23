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
    public class ExpiryTests
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

        #region expiry
        // TODO: Remove time sensitive element
        [TestMethod]
        public void SetExpiryThenGet()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            storageBuilder.WithExpiry(100);

            Dispatch(storageBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        // TODO: Remove time sensitive element
        [TestMethod]
        public void SetExpiryThenGetGone()
        {
            var storageBuilder = new StoreRequestBuilder("set", "key", "value");
            storageBuilder.WithExpiry(1);

            Dispatch(storageBuilder.ToAsciiRequest());

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(2));

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
        #endregion

    }
}
