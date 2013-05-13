using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests
{

    [TestClass]
    public class RequestResponseTcpServerTests
    {
        private RequestResponseTcpServer _server;

        [TestInitialize]
        public void Setup()
        {
            var address = IPAddress.Any;
            int port = 55555;
            _server = new RequestResponseTcpServer(address, port, null);
            _server.Start();
        }

        [TestMethod]
        public void CanConnectClientAndDisconnect()
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", 55555);

            Assert.IsTrue(tcpClient.Connected);

            tcpClient.Close();
        }


        [TestCleanup]
        public void Cleanup()
        {
            _server.Stop();
        }

    }
}
