using System;
using System.Linq;
using Nemcache.Storage.Notifications;

namespace Nemcache.Storage.Persistence
{
    public class BitcaskPersistence : ICachePersistence
    {
        private readonly BitcaskStore _store;
        private readonly MemCache _cache;

        public BitcaskPersistence(BitcaskStore store, MemCache cache)
        {
            _store = store;
            _cache = cache;
        }

        public void Restore()
        {
            foreach (var entry in _store.Entries())
            {
                _cache.Add(entry.Key, 0, DateTime.MaxValue, entry.Value);
            }
        }

        public void OnNext(ICacheNotification value)
        {
            switch (value)
            {
                case StoreNotification store:
                    _store.Put(store.Key, store.Data);
                    break;
                case RemoveNotification remove:
                    _store.Delete(remove.Key);
                    break;
                case ClearNotification:
                    foreach (var key in _store.Keys.ToArray())
                        _store.Delete(key);
                    break;
            }
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }
        public void Dispose() => _store.Dispose();
    }
}
