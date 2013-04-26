using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using System.Text;
using System.Reactive.Disposables;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class RequestHandlerTests
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
        public void Stats()
        {
            // TODO: 
            _requestHandler.Dispatch("endpoint", Encoding.ASCII.GetBytes("stats\r\n"), null);
        }

        [TestMethod]
        public void StatsSettings()
        {
            // TODO: 
            _requestHandler.Dispatch("endpoint", Encoding.ASCII.GetBytes("stats settings\r\n"), null);
        }

        [TestMethod]
        public void Quit()
        {
            bool disposed = false;
            _requestHandler.Dispatch("endpoint", Encoding.ASCII.GetBytes("quit\r\n"), Disposable.Create(() => { disposed = true; }));
            Assert.AreEqual(disposed, true);
        }
    }
}
