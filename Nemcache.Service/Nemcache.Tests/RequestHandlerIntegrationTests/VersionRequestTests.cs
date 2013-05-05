using System.Reactive.Concurrency;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class VersionRequestTests
    {
        private IClient _client;

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        public IClient Client { get; set; }

        [TestInitialize]
        public void Setup()
        {
            _client = Client ?? new LocalRequestHandlerWithTestScheduler();
        }

        [TestMethod]
        public void Version()
        {
            var flushRequest = Encoding.ASCII.GetBytes("version\r\n");
            var response = Dispatch(flushRequest);
            Assert.AreEqual("Nemcache 1.0.0.0\r\n", response.ToAsciiString());
        }
    }
}