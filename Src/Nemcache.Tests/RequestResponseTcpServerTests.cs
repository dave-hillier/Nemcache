using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using Nemcache.Service.RequestHandlers;

namespace Nemcache.Tests
{
    [TestClass]
    public class RequestResponseTcpServerTests
    {
        private RequestResponseTcpServer _server;

        public class PingPongHandler : IRequestHandler
        {
            public void HandleRequest(IRequestContext context)
            {
                using (var sw = new StreamWriter(context.ResponseStream))
                {
                    sw.WriteLine("Pong!\r\n");
                }
            }
        }
        [TestInitialize]
        public void Setup()
        {
            var address = IPAddress.Any;
            int port = 55555;
            IScheduler scheduler = Scheduler.Default;
            var requestHandlers = new Dictionary<string, IRequestHandler>()
                {
                    {"Ping!", new PingPongHandler()}
                };
            var requestDispatcher = new RequestDispatcher(scheduler, null, requestHandlers);
            _server = new RequestResponseTcpServer(address, port,
                requestDispatcher);
            _server.Start();
        }

        [TestMethod]
        public void CanConnectClientAndDisconnect()
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", 55555);
            Assert.IsTrue(tcpClient.Connected);
            tcpClient.Close();

            RequestResponseTest();
        }

        [TestMethod]
        public void RequestResponseTest()
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", 55555);
            using (var stream = tcpClient.GetStream())
            using (var streamReader = new StreamReader(stream))
            using (var streamWriter = new StreamWriter(stream))
            {
                streamWriter.WriteLine("Ping!\r\n");
                streamWriter.Flush();
                var response = streamReader.ReadLine();
                Assert.AreEqual("Pong!", response);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _server.Stop();
        }
    }
}
