using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class WebSocketHandler : IWebSocketHandler
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public WebSocketHandler(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public void OnWebSocketConnected(WebSocket webSocket)
        {
            Task.WaitAll(
                SendLoop(webSocket),
                ReceiveLoop(webSocket));
        }

        private async Task ReceiveLoop(WebSocket webSocket)
        {
            var receiveBuffer = new byte[4096];
            while (webSocket.State == WebSocketState.Open)
            {
                var arraySegment = new ArraySegment<byte>(receiveBuffer);
                var receiveResult = await webSocket.ReceiveAsync(arraySegment, _cancellationTokenSource.Token);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", _cancellationTokenSource.Token);
                }
                else if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    // TODO: consider sending via an actor?
                    Console.WriteLine("Received: {0}", Encoding.ASCII.GetString(arraySegment.Array).Trim('\0'));
                }
            }
        }

        private async Task SendLoop(WebSocket webSocket)
        {
            // TODO: create a client message queue
            var sendQueue = new BlockingCollection<byte[]>
                {
                    Encoding.UTF8.GetBytes("hello"),
                    Encoding.UTF8.GetBytes("world"),
                    Encoding.UTF8.GetBytes("foo"),
                    Encoding.UTF8.GetBytes("bar")
                };// TODO: save this so it is acecssible from somewhere else somehow...

            foreach (var val in sendQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                if (webSocket.State != WebSocketState.Open)
                    break;
                var arraySegment = new ArraySegment<byte>(val);
                await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            }
        }
    }
}