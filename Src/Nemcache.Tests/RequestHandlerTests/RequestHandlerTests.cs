using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using NUnit.Framework;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestFixture]
    public class RequestHandlerTests
    {
        private IClient _client;

        public IClient Client { get; set; }

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        [SetUp]
        public void Setup()
        {
            _client = Client ?? new LocalRequestHandlerWithTestScheduler();
        }

        [Test]
        public void UnknownCommand()
        {
            var error = Dispatch(Encoding.ASCII.GetBytes("NotACommand\r\n"));

            Assert.AreEqual("ERROR Unknown command: NotACommand\r\n", Encoding.ASCII.GetString(error));
        }

        [Test]
        public void MalformedCommand()
        {
            var error = Dispatch(Encoding.ASCII.GetBytes("malformed command").Concat(new byte[512]).ToArray());

            Assert.AreEqual("SERVER ERROR New line not found\r\n", Encoding.ASCII.GetString(error));
        }

        [Test]
        public void TestExceptionCommand()
        {
            var error = Dispatch(Encoding.ASCII.GetBytes("exception\r\n"));

            Assert.AreEqual("SERVER ERROR test exception\r\n", Encoding.ASCII.GetString(error));
        }

        [Test]
        public void Quit()
        {
            // TODO:
            bool disposed = false;
            _client.OnDisconnect = (() => { disposed = true; });
            Dispatch(Encoding.ASCII.GetBytes("quit\r\n"));
            Assert.AreEqual(disposed, true);
        }

        [Test]
        public void Stats()
        {
            var result = Dispatch(Encoding.ASCII.GetBytes("stats\r\n"));
            var text = result.ToAsciiString();
            StringAssert.Contains("STAT version", text);
            StringAssert.Contains("STAT uptime", text);
            StringAssert.EndsWith("END\r\n", text);
        }

        [Test]
        public void StatsSettings()
        {
            var result = Dispatch(Encoding.ASCII.GetBytes("stats settings\r\n"));
            var text = result.ToAsciiString();
            StringAssert.Contains("STAT maxbytes", text);
            StringAssert.EndsWith("END\r\n", text);
        }
    }
}