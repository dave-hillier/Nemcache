using System.Reactive.Concurrency;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class VersionRequestTests
    {
        private RequestHandler _requestHandler;

        [TestInitialize]
        public void Setup()
        {
            _requestHandler = new RequestHandler(100000, Scheduler.Default);
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