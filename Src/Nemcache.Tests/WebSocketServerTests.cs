using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Nemcache.Storage;

using Nemcache.Service;
namespace Nemcache.Tests
{
    [TestFixture]
    public class WebSocketServerTests
    {
        private WebSocketServer _webSocketServer;
        private ClientWebSocket _clientWebSocket;
        private Uri _wsUri;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        [SetUp]
        public void Setup()
        {
            const string baseUrl = "localhost:8688/websocket/";
            const string httpUrl = "http://" + baseUrl;
            _wsUri = new Uri("ws://" + baseUrl + "someend");

            var cache = new MemCache(10000);
            _webSocketServer = new WebSocketServer(new[] { httpUrl }, observer => new CacheEntrySubscriptionHandler(cache, observer));
            _webSocketServer.Start();
            _clientWebSocket = new ClientWebSocket();
            _clientWebSocket.Options.AddSubProtocol("nemcache-0.1");
            _clientWebSocket.ConnectAsync(_wsUri, _cts.Token).Wait();
        }
        
        [TearDown]
        public void Cleanup()
        {
            _clientWebSocket.Abort();
            _webSocketServer.Stop();
        }

        // TODO: Immediate response test
        [Test]
        public void ClientSubscribesToKey()
        {
            // Send subscribe as text/json
            const string subscribeRequest = "{\"command\":\"subscribe\", \"key\":\"mykey\"}";
            var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(subscribeRequest));
            _clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token).Wait();

            var buffer = new ArraySegment<byte>(new byte[1024]);
            var response = _clientWebSocket.ReceiveAsync(buffer, _cts.Token).Result;
            var responseString = Encoding.UTF8.GetString(buffer.Array);

            // Expect an initial value - or no value if it doesnt exist?
            Assert.AreEqual(WebSocketMessageType.Text, response.MessageType);
            Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", responseString.Trim('\0'));
        }

        [Test]
        public void SendInTwoParts()
        {
            // Send subscribe as text/json
            var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"command\":\"subscribe\", \"k"));
            _clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, false, _cts.Token).Wait();

            var sendBuffer2 = new ArraySegment<byte>(Encoding.UTF8.GetBytes("ey\":\"mykey\"}"));
            _clientWebSocket.SendAsync(sendBuffer2, WebSocketMessageType.Text, true, _cts.Token).Wait();


            var buffer = new ArraySegment<byte>(new byte[1024]);
            var response = _clientWebSocket.ReceiveAsync(buffer, _cts.Token).Result;
            var responseString = Encoding.UTF8.GetString(buffer.Array);

            Assert.AreEqual(WebSocketMessageType.Text, response.MessageType);
            Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", responseString.Trim('\0'));
        }

        [Test]
        public void SendInThreeParts()
        {
            var part1 = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"command\":\"sub"));
            _clientWebSocket.SendAsync(part1, WebSocketMessageType.Text, false, _cts.Token).Wait();

            var part2 = new ArraySegment<byte>(Encoding.UTF8.GetBytes("scribe\",\"key\":\"my"));
            _clientWebSocket.SendAsync(part2, WebSocketMessageType.Text, false, _cts.Token).Wait();

            var part3 = new ArraySegment<byte>(Encoding.UTF8.GetBytes("key\"}"));
            _clientWebSocket.SendAsync(part3, WebSocketMessageType.Text, true, _cts.Token).Wait();

            var buffer = new ArraySegment<byte>(new byte[1024]);
            var response = _clientWebSocket.ReceiveAsync(buffer, _cts.Token).Result;
            var responseString = Encoding.UTF8.GetString(buffer.Array);

            Assert.AreEqual(WebSocketMessageType.Text, response.MessageType);
            Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", responseString.Trim('\0'));
        }

        [Test]
        public void DisconnectAndReconnect()
        {
            const string subscribeRequest = "{\"command\":\"subscribe\", \"key\":\"mykey\"}";
            var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(subscribeRequest));
            _clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token).Wait();
            var buffer = new ArraySegment<byte>(new byte[1024]);
            _clientWebSocket.ReceiveAsync(buffer, _cts.Token).Wait();

            _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", _cts.Token).Wait();
            _clientWebSocket.Dispose();

            _clientWebSocket = new ClientWebSocket();
            _clientWebSocket.Options.AddSubProtocol("nemcache-0.1");
            _clientWebSocket.ConnectAsync(_wsUri, _cts.Token).Wait();

            _clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token).Wait();
            buffer = new ArraySegment<byte>(new byte[1024]);
            var response = _clientWebSocket.ReceiveAsync(buffer, _cts.Token).Result;
            var responseString = Encoding.UTF8.GetString(buffer.Array);

            Assert.AreEqual(WebSocketMessageType.Text, response.MessageType);
            Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", responseString.Trim('\0'));
        }

        [Test]
        public void MultipleClients()
        {
            using (var other = new ClientWebSocket())
            {
                other.Options.AddSubProtocol("nemcache-0.1");
                other.ConnectAsync(_wsUri, _cts.Token).Wait();

                var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"command\":\"subscribe\", \"key\":\"mykey\"}"));
                _clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token).Wait();
                other.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token).Wait();

                var buffer1 = new ArraySegment<byte>(new byte[1024]);
                var resp1 = _clientWebSocket.ReceiveAsync(buffer1, _cts.Token).Result;
                var str1 = Encoding.UTF8.GetString(buffer1.Array);

                var buffer2 = new ArraySegment<byte>(new byte[1024]);
                var resp2 = other.ReceiveAsync(buffer2, _cts.Token).Result;
                var str2 = Encoding.UTF8.GetString(buffer2.Array);

                Assert.AreEqual(WebSocketMessageType.Text, resp1.MessageType);
                Assert.AreEqual(WebSocketMessageType.Text, resp2.MessageType);
                Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", str1.Trim('\0'));
                Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", str2.Trim('\0'));
            }
        }

        [Test]
        public void SendInThreeParts()
        {
            var part1 = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"command\":\"sub"));
            _clientWebSocket.SendAsync(part1, WebSocketMessageType.Text, false, _cts.Token).Wait();

            var part2 = new ArraySegment<byte>(Encoding.UTF8.GetBytes("scribe\",\"key\":\"my"));
            _clientWebSocket.SendAsync(part2, WebSocketMessageType.Text, false, _cts.Token).Wait();

            var part3 = new ArraySegment<byte>(Encoding.UTF8.GetBytes("key\"}"));
            _clientWebSocket.SendAsync(part3, WebSocketMessageType.Text, true, _cts.Token).Wait();

            var buffer = new ArraySegment<byte>(new byte[1024]);
            var response = _clientWebSocket.ReceiveAsync(buffer, _cts.Token).Result;
            var responseString = Encoding.UTF8.GetString(buffer.Array);

            Assert.AreEqual(WebSocketMessageType.Text, response.MessageType);
            Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", responseString.Trim('\0'));
        }

        [Test]
        public void DisconnectAndReconnect()
        {
            const string subscribeRequest = "{\"command\":\"subscribe\", \"key\":\"mykey\"}";
            var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(subscribeRequest));
            _clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token).Wait();
            var buffer = new ArraySegment<byte>(new byte[1024]);
            _clientWebSocket.ReceiveAsync(buffer, _cts.Token).Wait();

            _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", _cts.Token).Wait();
            _clientWebSocket.Dispose();

            _clientWebSocket = new ClientWebSocket();
            _clientWebSocket.Options.AddSubProtocol("nemcache-0.1");
            _clientWebSocket.ConnectAsync(_wsUri, _cts.Token).Wait();

            _clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token).Wait();
            buffer = new ArraySegment<byte>(new byte[1024]);
            var response = _clientWebSocket.ReceiveAsync(buffer, _cts.Token).Result;
            var responseString = Encoding.UTF8.GetString(buffer.Array);

            Assert.AreEqual(WebSocketMessageType.Text, response.MessageType);
            Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", responseString.Trim('\0'));
        }

        [Test]
        public void MultipleClients()
        {
            using (var other = new ClientWebSocket())
            {
                other.Options.AddSubProtocol("nemcache-0.1");
                other.ConnectAsync(_wsUri, _cts.Token).Wait();

                var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"command\":\"subscribe\", \"key\":\"mykey\"}"));
                _clientWebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token).Wait();
                other.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cts.Token).Wait();

                var buffer1 = new ArraySegment<byte>(new byte[1024]);
                var resp1 = _clientWebSocket.ReceiveAsync(buffer1, _cts.Token).Result;
                var str1 = Encoding.UTF8.GetString(buffer1.Array);

                var buffer2 = new ArraySegment<byte>(new byte[1024]);
                var resp2 = other.ReceiveAsync(buffer2, _cts.Token).Result;
                var str2 = Encoding.UTF8.GetString(buffer2.Array);

                Assert.AreEqual(WebSocketMessageType.Text, resp1.MessageType);
                Assert.AreEqual(WebSocketMessageType.Text, resp2.MessageType);
                Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", str1.Trim('\0'));
                Assert.AreEqual("{\"subscription\":\"mykey\",\"response\":\"OK\"}", str2.Trim('\0'));
            }
        }

        // TODO: sending message in multiple parts, endofmessage false, then true
        // TODO: delayed response test
        // TODO: multiple response test
        // TODO: multiple request test
        // TODO: disconnect test
        // TODO: multiple client test
        // TODO: some tests with real binary data

    }
}