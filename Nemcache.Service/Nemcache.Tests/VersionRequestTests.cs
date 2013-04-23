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
    public class VersionRequestTests
    {
        RequestHandler _requestHandler;
        TestScheduler _testScheduler;

        [TestInitialize]
        public void Setup()
        {
            _requestHandler = new RequestHandler(100000);
            Scheduler.Current = _testScheduler = new TestScheduler();
        }

        [TestMethod]
        public void Version()
        {
            var flushRequest = Encoding.ASCII.GetBytes("version\r\n");
            var response = _requestHandler.Dispatch("remote", flushRequest, null);
            Assert.AreEqual("Nemcache 1.0.0.0\r\n", response.ToAsciiString());
        }
    }    

}
