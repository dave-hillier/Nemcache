using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class RequestHandlerTests
    {
        private RequestHandler _requestHandler;

        [TestInitialize]
        public void Setup()
        {
            _requestHandler = new RequestHandler(100000, Scheduler.Default);
        }

        [TestMethod]
        public void UnknownCommand()
        {
            var error = _requestHandler.Dispatch("endpoint", Encoding.ASCII.GetBytes("unknown command\r\n"), null);

            Assert.AreEqual("ERROR\r\n", Encoding.ASCII.GetString(error));
        }

        [TestMethod]
        public void MalformedCommand()
        {
            var error = _requestHandler.Dispatch("endpoint", Encoding.ASCII.GetBytes("malformed command"), null);

            Assert.AreEqual("SERVER ERROR New line not found\r\n", Encoding.ASCII.GetString(error));
        }

        [TestMethod]
        public void TestExceptionCommand()
        {
            var error = _requestHandler.Dispatch("endpoint", Encoding.ASCII.GetBytes("exception\r\n"), null);

            Assert.AreEqual("SERVER ERROR test exception\r\n", Encoding.ASCII.GetString(error));
        }

        [TestMethod]
        public void Quit()
        {
            bool disposed = false;
            _requestHandler.Dispatch("endpoint", Encoding.ASCII.GetBytes("quit\r\n"),
                                     Disposable.Create(() => { disposed = true; }));
            Assert.AreEqual(disposed, true);
        }

        [TestMethod]
        public void Stats()
        {
            // TODO: implement
            var result = _requestHandler.Dispatch("endpoint", Encoding.ASCII.GetBytes("stats\r\n"), null);
        }

        [TestMethod]
        public void StatsSettings()
        {
            // TODO: implement
            var result = _requestHandler.Dispatch("endpoint", Encoding.ASCII.GetBytes("stats settings\r\n"), null);
        }

    }
}