using System.Net.WebSockets;

namespace Nemcache.Service
{
    internal interface IWebSocketHandler
    {
        void OnWebSocketConnected(WebSocket webSocket);
    }
}