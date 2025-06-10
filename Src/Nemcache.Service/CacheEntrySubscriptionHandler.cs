using Nemcache.Storage.Notifications;
using Nemcache.Storage.Reactive;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using Nemcache.Storage;
using System.Reactive.Linq;
using System.Text;

namespace Nemcache.Service
{
    // TODO: replace dictionaries with classes
    class CacheEntrySubscriptionHandler : IDisposable
    {
        private readonly IMemCache _cache;
        private readonly IObserver<string> _responseObserver;
        private readonly Dictionary<string, IDisposable> _subscriptions = new Dictionary<string, IDisposable>();

        public CacheEntrySubscriptionHandler(IMemCache cache, IObserver<string> responseObserver) // Not sure how right it 
        {
            _cache = cache;
            _responseObserver = responseObserver;
        }

        public void HandleMessage(string command)
        {
            var cmd = JsonObject.Parse(command);

            var commandName = cmd["command"];
            if (commandName == "subscribe")
            {
                Subscribe(cmd);
            }
            else if (commandName == "unsubscribe")
            {
                Unsubscribe(cmd);
            }
            //TODO: unknown cmd
        }

        private void Unsubscribe(JsonObject cmd)
        {
            var key = cmd["key"];
            if (_subscriptions.ContainsKey(key))
            {
                _subscriptions[key].Dispose();
                _subscriptions.Remove(key);
                SendUnsubscriptionConfirmation(_responseObserver, key);
            }
            else
            {
                SendUnsubscriptionError(_responseObserver, key);
            }
            else
            {
                SendUnsubscriptionError(_responseObserver, key);
            }
        }

        private void Subscribe(JsonObject cmd)
        {
            var key = cmd["key"];
            if (string.IsNullOrEmpty(key))
            {
                SendError(_responseObserver);
            }
            else
            {
                SendSubscriptionConfirmation(_responseObserver, key);
                Subscribe(_responseObserver, key);
            }
        }

        private void SendSubscriptionConfirmation(IObserver<string> responseObserver, string key)
        {
            var response = new Dictionary<string, string>
                {
                    {"subscription", key},
                    {"response", "OK"}
                };
            responseObserver.OnNext(JsonSerializer.SerializeToString(response));
        }

        private void Subscribe(IObserver<string> responseObserver, string key)
        {
            CacheEntry cacheEntry;
            if (_cache.TryGet(key, out cacheEntry))
            {
                var notification = new StoreNotification
                    {
                        Key = key,
                        Data = cacheEntry.Data,
                        EventId = cacheEntry.EventId,
                        Expiry = cacheEntry.Expiry,
                        Flags = cacheEntry.Flags,
                        Operation = StoreOperation.Add
                    };
                var combinedNotifications = Observable.Return(notification).Combine(_cache.Notifications); // TODO: must be a simpler way of doing this...

            {
                var responseValue = new Dictionary<string, string>
                    {
                        {"value", store.Key},
                        {"data", Encoding.UTF8.GetString(store.Data)}
                    };
                return JsonSerializer.SerializeToString(responseValue);
            }
            var remove = keyCacheNotification as RemoveNotification;
            if (remove != null)
            {
                var responseValue = new Dictionary<string, string>
                        {"remove", remove.Key}
                return JsonSerializer.SerializeToString(responseValue);
            }

            return string.Empty;
        }

        private static void SendUnsubscriptionConfirmation(IObserver<string> responseObserver, string key)
        {
            var response = new Dictionary<string, string>
                {
                    {"subscription", key},
                    {"response", "UNSUBSCRIBED"}
                };
            responseObserver.OnNext(JsonSerializer.SerializeToString(response));
        }

        private static void SendUnsubscriptionError(IObserver<string> responseObserver, string key)
        {
            var response = new Dictionary<string, string>
                {
                    {"subscription", key},
                    {"response", "ERROR"}
                };
            responseObserver.OnNext(JsonSerializer.SerializeToString(response));
            {
                _subscriptions[key] = _cache.Notifications.
                                                OfType<IKeyCacheNotification>().
                                                Where(k => k.Key == key).
                                                Select(JsonFromNotifications).
                                                Subscribe(responseObserver);                
            }

        }

        private static void SendError(IObserver<string> responseObserver)
        {
            var response = new Dictionary<string, string>
                {
                    {"subscription", ""},
                    {"response", "ERROR"}
                };
            responseObserver.OnNext(JsonSerializer.SerializeToString(response));
        }

        private string JsonFromNotifications(IKeyCacheNotification keyCacheNotification)
        {
            var store = keyCacheNotification as StoreNotification;
            if (store != null)
            {
                var responseValue = new Dictionary<string, string>
                    {
                        {"value", store.Key},
                        {"data", Encoding.UTF8.GetString(store.Data)}
                    };
                return JsonSerializer.SerializeToString(responseValue);
            }

            var remove = keyCacheNotification as RemoveNotification;
            if (remove != null)
            {
                var responseValue = new Dictionary<string, string>
                    {
                        {"remove", remove.Key}
                    };
                return JsonSerializer.SerializeToString(responseValue);
            }

            return string.Empty;
        }

        private static void SendUnsubscriptionConfirmation(IObserver<string> responseObserver, string key)
        {
            var response = new Dictionary<string, string>
                {
                    {"subscription", key},
                    {"response", "UNSUBSCRIBED"}
                };
            responseObserver.OnNext(JsonSerializer.SerializeToString(response));
        }

        private static void SendUnsubscriptionError(IObserver<string> responseObserver, string key)
        {
            var response = new Dictionary<string, string>
                {
                    {"subscription", key},
                    {"response", "ERROR"}
                };
            responseObserver.OnNext(JsonSerializer.SerializeToString(response));
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }
        }
    }
}