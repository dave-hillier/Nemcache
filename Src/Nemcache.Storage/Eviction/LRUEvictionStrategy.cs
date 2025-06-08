using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Nemcache.Storage.Notifications;

namespace Nemcache.Storage.Eviction
{
    internal class LruEvictionStrategy : IEvictionStrategy
    {
        private readonly MemCache _cache;
        private readonly List<string> _keys = new List<string>();
        private readonly IDisposable _subscriptions;

        public LruEvictionStrategy(MemCache cache)
        {
            _cache = cache;
            var notifications = _cache.Notifications;

            var removeSubscription = notifications.OfType<RemoveNotification>().Subscribe(n => Remove(n.Key));
            var touchSubscription = notifications.OfType<IKeyCacheNotification>().Where(n => !(n is RemoveNotification)).Subscribe(n => Use(n.Key));
            _subscriptions = new CompositeDisposable {removeSubscription, touchSubscription};
        }

        public void Use(string key)
        {
            lock (_keys)
            {
                _keys.Remove(key);
                _keys.Add(key);
            }
        }

        public void Remove(string key)
        {
            lock (_keys)
            {
                _keys.Remove(key);
            }
        }

        public void EvictEntry()
        {
            lock (_keys)
            {
                if (_keys.Any())
                {
                    _cache.Remove(_keys[0]);
                }
            }
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}