using System.Reactive.Disposables;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class RequestHandlerTests
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
        public void UnknownCommand()
        {
            var error = Dispatch(Encoding.ASCII.GetBytes("unknown command\r\n"));

            Assert.AreEqual("ERROR\r\n", Encoding.ASCII.GetString(error));
        }

        [TestMethod]
        public void MalformedCommand()
        {
            var error = Dispatch(Encoding.ASCII.GetBytes("malformed command"));

            Assert.AreEqual("SERVER ERROR New line not found\r\n", Encoding.ASCII.GetString(error));
        }

        [TestMethod]
        public void TestExceptionCommand()
        {
            var error = Dispatch(Encoding.ASCII.GetBytes("exception\r\n"));

            Assert.AreEqual("SERVER ERROR test exception\r\n", Encoding.ASCII.GetString(error));
        }

        [TestMethod]
        public void Quit()
        {
            // TODO:
            bool disposed = false;
            _client.OnDisconnect = Disposable.Create(() => { disposed = true; });
            Dispatch(Encoding.ASCII.GetBytes("quit\r\n"));
            Assert.AreEqual(disposed, true);
        }

        [TestMethod]
        public void Stats()
        {
            // TODO: implement
            var result = Dispatch(Encoding.ASCII.GetBytes("stats\r\n"));
        }

        [TestMethod]
        public void StatsSettings()
        {
            // TODO: implement
            var result = Dispatch(Encoding.ASCII.GetBytes("stats settings\r\n"));
        }
    }
}