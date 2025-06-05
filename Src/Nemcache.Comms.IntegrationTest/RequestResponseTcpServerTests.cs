using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using Nemcache.Service;

namespace Nemcache.Comms.IntegrationTest
{

    [TestFixture]
    public class RequestResponseTcpServerTests
    {
        private RequestResponseTcpServer _server;

        [SetUp]
        public void Setup()
        {
            var address = IPAddress.Any;
            int port = 55555;
            _server = new RequestResponseTcpServer(address, port, null);
            _server.Start();
        }

        [Test]
        public void CanConnectClientAndDisconnect()
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", 55555);

            Assert.IsTrue(tcpClient.Connected);

            tcpClient.Close();
        }


        [TearDown]
        public void Cleanup()
        {
            _server.Stop();
        }

    }
}
