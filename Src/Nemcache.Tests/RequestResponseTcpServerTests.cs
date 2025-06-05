using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using NUnit.Framework;
using Nemcache.Service;
using Nemcache.Service.RequestHandlers;

namespace Nemcache.Tests
{
    [TestFixture]
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
        [SetUp]
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

        [Test]
        public void CanConnectClientAndDisconnect()
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", 55555);
            Assert.IsTrue(tcpClient.Connected);
            tcpClient.Close();

            RequestResponseTest();
        }

        [Test]
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

        [TearDown]
        public void Cleanup()
        {
            _server.Stop();
        }
    }
}
