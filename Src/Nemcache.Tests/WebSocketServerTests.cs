using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class WebSocketServerTests
    {
        private WebSocketServer _webSocketServer;
        private ClientWebSocket _clientWebSocket;
        private Uri _wsUri;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        [TestInitialize]
        public void Setup()
        {
            const string baseUrl = "localhost:8688/websocket/";
            const string httpUrl = "http://" + baseUrl;
            _wsUri = new Uri("ws://" + baseUrl + "someend");

            _webSocketServer = new WebSocketServer(new[] { httpUrl });
            _webSocketServer.Start();
            _clientWebSocket = new ClientWebSocket();
            _clientWebSocket.Options.AddSubProtocol("nemcache-0.1");
            _clientWebSocket.ConnectAsync(_wsUri, _cts.Token).Wait();
        }
        
        [TestCleanup]
        public void Cleanup()
        {
            _clientWebSocket.Abort();
            _webSocketServer.Stop();
        }

        // TODO: Immediate response test
        [TestMethod]
        public void ClientSubscribesToKey()
        {
            // Send subscribe as text/json
            const string subscribeRequest = "{'command':'subscribe', 'key':'mykey'}";
            var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(subscribeRequest));
            _clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, false, _cts.Token);

            var buffer = new ArraySegment<byte>(new byte[1024]);
            var response = _clientWebSocket.ReceiveAsync(buffer, _cts.Token).Result;

            // Expect an initial value - or no value if it doesnt exist?
            Assert.AreEqual(WebSocketMessageType.Text, response.MessageType);
        }

        // TODO: delayed response test

        // TODO: multiple response test

        // TODO: multiple request test
    }
}