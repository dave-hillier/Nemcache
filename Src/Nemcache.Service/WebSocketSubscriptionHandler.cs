using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Nemcache.Service.Notifications;
using ServiceStack.Text;

namespace Nemcache.Service
{
    internal class RequestMessage
    {
        public BlockingCollection<byte[]> ResponseQueue { get; set; }
        public string Message { get; set; }
    }

    // TODO: Is this named well? It inteprets commands and setups subscriptions?
    // One per client? Should there be lots of these?
    class WebSocketSubscriptionHandler
    {
        private readonly IMemCache _cache;
        private readonly Dictionary<Tuple<object, string>, IDisposable> _subscriptions = new Dictionary<Tuple<object, string>, IDisposable>();

        public WebSocketSubscriptionHandler(IMemCache cache)
        {
            _cache = cache;
        }

        public void HandleMessage(string command, IObserver<string> responseObserver)
        {
            var cmd = JsonObject.Parse(command);

            var commandName = cmd["command"];
            if (commandName == "subscribe")
            {
                Subscribe(cmd, responseObserver);
            }
            else if (commandName == "unsubscribe")
            {
                Unsubscribe(cmd, responseObserver);
            }
        }

        private void Unsubscribe(JsonObject cmd, IObserver<string> responseObserver)
        {
            var key = cmd["key"];
            var subKey = Tuple.Create((object) responseObserver, key);
            if (_subscriptions.ContainsKey(subKey))
            {
                _subscriptions[subKey].Dispose();
                _subscriptions.Remove(subKey);
            }
            // TODO: unsubscribe success?
        }

        private void Subscribe(JsonObject cmd, IObserver<string> responseObserver)
        {
            var key = cmd["key"];
            var subKey = Tuple.Create((object)responseObserver, key);
            if (string.IsNullOrEmpty(key))
            {
                var response = new Dictionary<string, string>()
                    {
                        {"subscription", ""},
                        {"response", "ERROR"}
                    };
                responseObserver.OnNext(JsonSerializer.SerializeToString(response));
            }
            else
            {
                var response = new Dictionary<string, string>()
                    {
                        {"subscription", key},
                        {"response", "OK"}
                    };
                responseObserver.OnNext(JsonSerializer.SerializeToString(response));

                // TODO: concurrency?
                _subscriptions[subKey] = _cache.FullStateNotifications. // TODO: subscribe at start? Seems wrong for every client?
                       OfType<IKeyCacheNotification>().
                       Where(k => k.Key == key).
                       Select(n => JsonFromNotifications(n)).
                       Subscribe(responseObserver);
            }
        }

        private string JsonFromNotifications(IKeyCacheNotification keyCacheNotification)
        {
            var data = new byte[0];
            var store = keyCacheNotification as StoreNotification;
            if (store != null)
                data = store.Data;
            // TODO: removes

            var responseValue = new Dictionary<string, string>()
                    {
                        {"value", keyCacheNotification.Key},
                        {"data", Encoding.UTF8.GetString(data)}
                    };
            return responseValue.ToJson();
        }        
    }
}