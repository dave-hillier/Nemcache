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
    class ReaderLock : IDisposable
    {
        private ReaderWriterLockSlim _cacheLock;

        public ReaderLock(ReaderWriterLockSlim cacheLock)
        {
            _cacheLock = cacheLock;
            _cacheLock.EnterReadLock();
        }
        public void Dispose()
        {
            _cacheLock.ExitReadLock();
        }
    }

    class WriterLock : IDisposable
    {
        private ReaderWriterLockSlim _cacheLock;

        public ReaderLock(ReaderWriterLockSlim cacheLock)
        {
            _cacheLock = cacheLock;
            _cacheLock.EnterWriteLock();
        }
        public void Dispose()
        {
            _cacheLock.ExitWriteLock();
        }
    }

    internal class MemCache : IMemCache 
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        private readonly IEvictionStrategy _evictionStrategy;
        private readonly ICacheObserver _cacheObserver;
        private ReplaySubject<ICacheNotification> _notifications;

        public MemCache(int capacity)
        {
            Capacity = capacity;
            _notifications = new ReplaySubject<ICacheNotification>();
            //_evictionStrategy = new RandomEvictionStrategy(this); // TODO: inject
            var lruStrategy = new LRUEvictionStrategy(this);
            _evictionStrategy = lruStrategy;
            _cacheObserver = lruStrategy;
        }

        public struct CacheEntry
        {
            public ulong Flags { get; set; }
            public DateTime Inserted { get; set; }
            public DateTime Expiry { get; set; }
            public ulong CasUnique { get; set; }
            public byte[] Data { get; set; }

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
            resultDataOut = resultData;
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
                    var newValue = new CacheEntry
                    {
                        Inserted = Scheduler.Current.Now,
                        CasUnique = casUnique,
                        Data = newData,
                        Expiry = exptime,
                        Flags = flags
                    };
                    return _cache.TryUpdate(key, newValue, entry);
                }
                else
                {
                    return false;
                }
            }

            _cacheObserver.Use(key);
            _cache[key] = new CacheEntry
            {
                Inserted = Scheduler.Current.Now,
                CasUnique = casUnique,
                Data = newData,
                Expiry = exptime,
                Flags = flags
            };
            return true;
        }

        public bool Replace(string key, ulong flags, DateTime exptime, byte[] data)
        {
            _cacheObserver.Use(key);
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            return _cache.TryUpdate(key, e =>
            {
                return new CacheEntry
                {
                    Inserted = Scheduler.Current.Now,
                    Data = data,
                    Expiry = exptime,
                    Flags = flags
                };
            });
        }

        public bool Add(string key, ulong flags, DateTime exptime, byte[] data)
        {
            _cacheObserver.Use(key);
            _notifications.OnNext(new Store { Key = key, Data = data, Expiry = exptime, Flags = flags, Operation = StoreOperation.Add });
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value

            var entry = new CacheEntry
            {
                Inserted = Scheduler.Current.Now,
                Data = data,
                Expiry = exptime,
                Flags = flags
            };
            return _cache.TryAdd(key, entry);
        }

        public bool Append(string key, ulong flags, DateTime exptime, byte[] data, bool prepend)
        {
            _cacheObserver.Use(key);
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            var exists = _cache.TryUpdate(key, e =>
            {
                var newEntry = e;
                newEntry.Data = prepend ?
                    e.Data.Concat(data).ToArray() :
                    data.Concat(e.Data).ToArray();
                return newEntry;
            });
            return exists || Store(key, flags, exptime, data);
        }

        public bool Store(string key, ulong flags, DateTime exptime, byte[] data)
        {
            _cacheObserver.Use(key);
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            _cache[key] = new CacheEntry { 
                Data = data, 
                Expiry = exptime, 
                Flags = flags };
            return true;
        }

        public bool Remove(string key)
        {
            _cacheObserver.Remove(key);
            CacheEntry entry;
            return _cache.TryRemove(key, out entry);
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
            get { return _notifications; }
        }

        public class CacheState
        {
            int Version { get; set; }
            IEnumerable<KeyValuePair<string, CacheEntry>> State { get; set; }
        }

        internal CacheState GetCurrentState()
        {
            // TODO: atomically get the entire state!
            throw new NotImplementedException();
        }
    }
}
