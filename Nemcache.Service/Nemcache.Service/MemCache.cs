using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;

namespace Nemcache.Service
{
    internal class MemCache : IMemCache 
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        private readonly IEvictionStrategy _evictionStrategy;
        private readonly ICacheObserver _cacheObserver;
        private IObservable<ICacheNotification> _notificationsAndHistory;
        private Subject<ICacheNotification> _notificationsSubject;
        private int _currentSequenceId = 0;

        public MemCache(int capacity)
        {
            Capacity = capacity;
            _notificationsSubject = new Subject<ICacheNotification>();

            _notificationsAndHistory = CreateNotifications();

            var lruStrategy = new LRUEvictionStrategy(this);
            _evictionStrategy = lruStrategy;
            _cacheObserver = lruStrategy;
        }

        private IObservable<ICacheNotification> CreateNotifications()
        {
            return Observable.Create<ICacheNotification>(obs =>
            {
                var currentCache = _cache.ToArray();
                var maxCurrentCachedId = currentCache.Length > 0 ? currentCache.Max(ce => ce.Value.SequenceId) : -1;
                var addOperations = currentCache.Select(e =>
                    new Store
                    {
                        Key = e.Key,
                        Data = e.Value.Data,
                        Expiry = e.Value.Expiry,
                        Flags = e.Value.Flags,
                        Operation = StoreOperation.Add,
                        SequenceId = e.Value.SequenceId
                    });


                // TODO: this can go wrong if we get an update to a key before the initial value arrives. 
                // Perhaps send notifications through a queue to ensure ordering.
                var d = _notificationsSubject.
                    Where(n => n.SequenceId > maxCurrentCachedId).
                    Subscribe(obs);

                foreach (var n in addOperations)
                {
                    obs.OnNext(n);
                }

                return d;
            });
            
        }

        public struct CacheEntry
        {
            public ulong Flags { get; set; }
            public DateTime Expiry { get; set; }
            public ulong CasUnique { get; set; }
            public byte[] Data { get; set; }
            public int SequenceId { get; set; }

            public bool IsExpired { get { return Expiry < Scheduler.Current.Now; } }
        }

        public int Capacity { get; set; }

        public int Used
        {
            get
            {
                return _cache.Values.Select(e => e.Data.Length).Sum();
            }
        }

        public IEnumerable<string> Keys { get { return _cache.Keys; } }

        public void Clear()
        {
            _cache.Clear();
        }

        public bool Touch(string key, DateTime exptime)
        {
            bool success = false;
            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                CacheEntry touched = entry;
                touched.Expiry = exptime;
                _cacheObserver.Use(key); 
                _cache.TryUpdate(key, touched, entry); // OK to fail if something else has updated?
                success = true;
                // TODO: Notify
            }
            return success;
        }

        // I dont like this method as it assumes a general format.
        public bool Mutate(string commandName, string key, ulong incr, out byte[] resultDataOut)
        {
            byte[] resultData = null;
            bool result = _cache.TryUpdate(key,
            entry =>
            {
                var value = ulong.Parse(Encoding.ASCII.GetString(entry.Data));
                if (commandName == "incr")
                    value += incr;
                else
                    value -= incr;
                resultData = Encoding.ASCII.GetBytes(value.ToString());
                entry.Data = resultData;
                return entry;
            });

            if (result)
            {
                resultDataOut = resultData;
                // TODO: notify
            }
            else
            {
                resultDataOut = null;
            }
            return result;
        }

        public void MakeSpaceForNewEntry(int length)
        {
            while (!HasAvailableSpace(length))
            {
                _evictionStrategy.EvictEntry();
            }
        }

        private bool HasAvailableSpace(int length)
        {
            return Capacity >= Used + length;
        }

        public bool Cas(string key, ulong flags, DateTime exptime, ulong casUnique, byte[] newData)
        {
            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                if (entry.CasUnique == casUnique)
                {
                    var spaceRequired = Math.Abs(newData.Length - entry.Data.Length);
                    if (spaceRequired > 0)
                        MakeSpaceForNewEntry(spaceRequired);
                    _cacheObserver.Use(key);
                    var sequenceId = Interlocked.Increment(ref _currentSequenceId);
                    var newValue = new CacheEntry
                    {
                        CasUnique = casUnique,
                        Data = newData,
                        Expiry = exptime,
                        Flags = flags,
                        SequenceId = sequenceId,
                    };
                    var updated = _cache.TryUpdate(key, newValue, entry);
                    // notify cas update
                    return updated;
                }
                else
                {
                    return false;
                }
            }
            CasStore(key, flags, exptime, casUnique, newData);
            return true;
        }

        private void CasStore(string key, ulong flags, DateTime exptime, ulong casUnique, byte[] newData)
        {

            _cacheObserver.Use(key);
            var sequenceId2 = Interlocked.Increment(ref _currentSequenceId);
            _cache[key] = new CacheEntry
            {
                CasUnique = casUnique,
                Data = newData,
                Expiry = exptime,
                Flags = flags,
                SequenceId = sequenceId2,
            };
            // TODO: notfiy
        }

        public bool Replace(string key, ulong flags, DateTime exptime, byte[] data)
        {
            _cacheObserver.Use(key);
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            var replaced = _cache.TryUpdate(key, e =>
            {
                var sequenceId = Interlocked.Increment(ref _currentSequenceId);                
                return new CacheEntry
                {
                    Data = data,
                    Expiry = exptime,
                    Flags = flags,
                    SequenceId = sequenceId
                };
            });
            // TODO: Notify if replaced - and how to get the value

            return replaced;
        }

        public bool Add(string key, ulong flags, DateTime exptime, byte[] data)
        {
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            var sequenceId = Interlocked.Increment(ref _currentSequenceId);
            var entry = new CacheEntry
            {
                Data = data,
                Expiry = exptime,
                Flags = flags,
                SequenceId = sequenceId
            };
            bool result = _cache.TryAdd(key, entry);

            if (result)
            {
                _cacheObserver.Use(key);
                _notificationsSubject.OnNext(new Store 
                { 
                    Key = key, 
                    Data = data, 
                    Expiry = exptime, 
                    Flags = flags, 
                    Operation = StoreOperation.Add, 
                    SequenceId = sequenceId 
                });
            }
            return result;
        }

        public bool Append(string key, ulong flags, DateTime exptime, byte[] data, bool prepend)
        {
            _cacheObserver.Use(key);
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            var exists = _cache.TryUpdate(key, e =>
            {
                var sequenceId = Interlocked.Increment(ref _currentSequenceId);
                var newEntry = e;
                newEntry.Data = prepend ?
                    e.Data.Concat(data).ToArray() :
                    data.Concat(e.Data).ToArray();
                newEntry.SequenceId = sequenceId;
                return newEntry;
            });
            if (exists)
            {
                // TODO: notify append
            }
            return exists || Store(key, flags, exptime, data);
        }

        public bool Store(string key, ulong flags, DateTime exptime, byte[] data)
        {
            _cacheObserver.Use(key);
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            var sequenceId = Interlocked.Increment(ref _currentSequenceId);
            _cache[key] = new CacheEntry
            { 
                Data = data, 
                Expiry = exptime, 
                Flags = flags,
                SequenceId = sequenceId
            };
            _notificationsSubject.OnNext(
                new Store { 
                    Key = key, 
                    Data = data, 
                    Expiry = exptime, 
                    Flags = flags, 
                    Operation = StoreOperation.Store, 
                    SequenceId = sequenceId
                });
            return true;
        }

        public bool Remove(string key)
        {
            _cacheObserver.Remove(key);
            CacheEntry entry;
            var sequenceId = Interlocked.Increment(ref _currentSequenceId);
            bool removed = _cache.TryRemove(key, out entry);
            if (removed)
                _notificationsSubject.OnNext(new Remove { Key = key, SequenceId = sequenceId });
            return removed;
        }

        public IEnumerable<KeyValuePair<string, CacheEntry>> Retrieve(IEnumerable<string> keys)
        {
            var tmp = keys.ToArray();
            foreach (var key in tmp)
            {
                _cacheObserver.Use(key);
            }
            return from key in tmp
                   where _cache.ContainsKey(key)
                   select KeyValuePair.Create(key, _cache[key]);
        }

        public IObservable<ICacheNotification> Notifications
        {
            get { return _notificationsAndHistory; }
        }
    }
}
