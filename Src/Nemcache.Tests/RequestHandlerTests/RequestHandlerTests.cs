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

            Assert.That(Encoding.ASCII.GetString(error), Is.EqualTo("ERROR Unknown command: NotACommand\r\n"));
        }

        [Test]
        public void MalformedCommand()
        {
            var error = Dispatch(Encoding.ASCII.GetBytes("malformed command").Concat(new byte[512]).ToArray());

            Assert.That(Encoding.ASCII.GetString(error), Is.EqualTo("SERVER ERROR New line not found\r\n"));
        }

        [Test]
        public void TestExceptionCommand()
        {
            var error = Dispatch(Encoding.ASCII.GetBytes("exception\r\n"));

            Assert.That(Encoding.ASCII.GetString(error), Is.EqualTo("SERVER ERROR test exception\r\n"));
        }

        [Test]
        public void Quit()
        {
            // TODO:
            bool disposed = false;
            _client.OnDisconnect = (() => { disposed = true; });
            Dispatch(Encoding.ASCII.GetBytes("quit\r\n"));
            Assert.That(true, Is.EqualTo(disposed));
        }

        [Test]
        public void Stats()
        {
            var result = Dispatch(Encoding.ASCII.GetBytes("stats\r\n"));
            var text = result.ToAsciiString();
            Assert.That(text, Does.Contain("STAT version"));
            Assert.That(text, Does.Contain("STAT uptime"));
            Assert.That(text, Does.EndWith("END\r\n"));
        }

        [Test]
        public void StatsSettings()
        {
            var result = Dispatch(Encoding.ASCII.GetBytes("stats settings\r\n"));
            var text = result.ToAsciiString();
            Assert.That(text, Does.Contain("STAT maxbytes"));
            Assert.That(text, Does.EndWith("END\r\n"));
        }
    }
}