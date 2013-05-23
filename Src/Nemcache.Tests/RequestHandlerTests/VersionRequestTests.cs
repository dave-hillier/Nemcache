using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class VersionRequestTests
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

        [TestMethod]
        public void Version()
        {
            var flushRequest = Encoding.ASCII.GetBytes("version\r\n");
            var response = Dispatch(flushRequest);
            Assert.AreEqual("Nemcache 1.0.0.0\r\n", response.ToAsciiString());
        }
    }
}