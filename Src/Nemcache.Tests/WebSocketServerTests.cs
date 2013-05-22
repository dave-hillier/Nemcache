using System;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class WebSocketServerTests
    {
        private WebSocketServer _webSocketServer;
        private ClientWebSocket _cws;
        private Uri _wsUri;
        private CancellationTokenSource _cts;

        [TestInitialize]
        public void Setup()
        {
            const string baseUrl = "localhost:8688/websocket/";
            const string httpUrl = "http://" + baseUrl;
            _wsUri = new Uri("ws://" + baseUrl + "someend");
            _cts = new CancellationTokenSource();

            var webSocketHandler = new WebSocketHandler(_cts);
            _webSocketServer = new WebSocketServer(webSocketHandler, new[] { httpUrl });
            _webSocketServer.Start();
            _cws = new ClientWebSocket();
            _cws.Options.AddSubProtocol("nemcache-0.1");
        }

        [TestMethod]
        public void ConnectWebSocketDoesNotThrow()
        {
            _cws.ConnectAsync(_wsUri, _cts.Token).Wait();
        }

        [TestMethod]
        public void Receive()
        {
            _cws.ConnectAsync(_wsUri, _cts.Token).Wait();

            var buffer = new ArraySegment<byte>(new byte[1024]);
            var r = _cws.ReceiveAsync(buffer, _cts.Token).Result;
        }
    }
}