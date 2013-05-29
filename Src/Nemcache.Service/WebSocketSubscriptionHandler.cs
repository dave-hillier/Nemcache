using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Nemcache.Service.Notifications;
using ServiceStack.Text;

namespace Nemcache.Service
{
    // TODO: Is this named well? It inteprets commands and setups subscriptions?
    // One per client? Should there be lots of these?
    class WebSocketSubscriptionHandler : ISubject<string>
    {
        private readonly IMemCache _cache;
        readonly Subject<string> _response = new Subject<string>();
        private Dictionary<string,IDisposable> _subscriptions = new Dictionary<string, IDisposable>();

        public WebSocketSubscriptionHandler(IMemCache cache)
        {
            _cache = cache;
        }

        public void OnNext(string command)
        {
            var cmd = JsonObject.Parse(command);

            var commandName = cmd["command"];
            if (commandName == "subscribe")
            {
                Subscribe(cmd);
            }
            else if (commandName == "unsubscribe")
            {
                var key = cmd["key"];
                if (_subscriptions.ContainsKey(key))
                {
                    _subscriptions[key].Dispose();
                    _subscriptions.Remove(key);
                }

            }
        }

        private void Subscribe(JsonObject cmd)
        {
            var key = cmd["key"];
            if (string.IsNullOrEmpty(key))
            {
                var response = new Dictionary<string, string>()
                    {
                        {"subscription", ""},
                        {"response", "ERROR"}
                    };
                _response.OnNext(JsonSerializer.SerializeToString(response));
            }
            else
            {
                var response = new Dictionary<string, string>()
                    {
                        {"subscription", key},
                        {"response", "OK"}
                    };
                _response.OnNext(JsonSerializer.SerializeToString(response));

                _subscriptions[key] = _cache.FullStateNotifications. // TODO: subscribe at start?
                       OfType<IKeyCacheNotification>().
                       Where(k => k.Key == key).
                       Subscribe(n => _response.OnNext(JsonFromNotifications(n)));
            }
        }

        private string JsonFromNotifications(IKeyCacheNotification keyCacheNotification)
        {
            var data = new byte[0];
            var store = keyCacheNotification as StoreNotification;
            if (store != null)
                data = store.Data;

            var responseValue = new Dictionary<string, string>()
                    {
                        {"value", keyCacheNotification.Key},
                        {"data", Encoding.UTF8.GetString(data)}
                    };
            return responseValue.ToJson();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return _response.Subscribe(observer);
        }
    }
}