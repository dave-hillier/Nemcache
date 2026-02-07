using System.Text;
using NUnit.Framework;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    /// <summary>
    /// Bug: VersionHandler returns "Nemcache {version}\r\n" instead of
    /// "VERSION {version}\r\n". Per the memcached protocol, the response
    /// must begin with the "VERSION " prefix.
    /// See VersionHandler.cs line 10.
    /// </summary>
    [TestFixture]
    public class VersionResponseBugTest
    {
        private IClient _client;

        private byte[] Dispatch(byte[] p) => _client.Send(p);

        [SetUp]
        public void Setup()
        {
            _client = new LocalRequestHandlerWithTestScheduler();
        }

        [Test]
        public void VersionResponseShouldStartWithVersionPrefix()
        {
            var request = Encoding.ASCII.GetBytes("version\r\n");
            var response = Dispatch(request);
            var responseStr = response.ToAsciiString();

            // Per memcached protocol, the response must begin with "VERSION "
            Assert.IsTrue(responseStr.StartsWith("VERSION "),
                $"Version response should start with 'VERSION ' prefix but was: '{responseStr}'");
        }
    }
}
