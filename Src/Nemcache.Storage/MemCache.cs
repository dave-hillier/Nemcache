﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using Nemcache.Storage.Eviction;
using Nemcache.Storage.Notifications;

namespace Nemcache.Storage
{
    public class MemCache : IMemCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache =
            new ConcurrentDictionary<string, CacheEntry>();
        private readonly IEvictionStrategy _evictionStrategy;
        private readonly IScheduler _scheduler;
        private readonly Subject<ICacheNotification> _notificationsSubject;
        private int _currentSequenceId;
        private int _used;
        private readonly IDisposable _interval;

        public MemCache(ulong capacity) : this(capacity, Scheduler.Default)
        {
            
        }

        public MemCache(ulong capacity, IScheduler scheduler)
        {
            _scheduler = scheduler;
            Capacity = capacity;
            _notificationsSubject = new Subject<ICacheNotification>();

            var lruStrategy = new LruEvictionStrategy(this);
            _evictionStrategy = lruStrategy;

            // TODO: replace this with some other kind of scheduling. Perhaps a sorted list...
            _interval = Observable.Interval(TimeSpan.FromSeconds(1), _scheduler).
                Subscribe(_ => RemoveExpired());

        }

        private void RemoveExpired()
        {
            var expired = _cache.ToArray().Where(ce => ce.Value.IsExpired(_scheduler)).ToArray();

            foreach (var kv in expired)
            {
                Remove(kv.Key);
            }
        }

        public ulong Capacity { get; set; }

        public ulong Used { get { return (ulong) _used; } }

        public void Clear()
        {
            int eventId = Interlocked.Increment(ref _currentSequenceId);
            _cache.Clear();

            _notificationsSubject.OnNext(new ClearNotification {EventId = eventId});
        }

        public bool Touch(string key, DateTime exptime)
        {
            bool success = false;
            int eventId = 0;
            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                eventId = Interlocked.Increment(ref _currentSequenceId);
                CacheEntry touched = entry;
                touched.EventId = eventId;
                touched.Expiry = exptime;
                _cache.TryUpdate(key, touched, entry); // OK to fail if something else has updated?
                success = true;
            }

            if (success)
            {
                _notificationsSubject.OnNext(new TouchNotification
                    {
                        Key = key,
                        Expiry = exptime,
                        EventId = eventId
                    });
            }
            return success;
        }

        // I dont like this method as it assumes a format of the data.
        public bool Mutate(string key, ulong incr, out byte[] resultDataOut, bool positive)
        {
            byte[] resultData = null;
            bool result = _cache.TryUpdate(
                key, entry =>
                    {
                        var value = ulong.Parse(Encoding.ASCII.GetString(entry.Data));
                        if (positive)
                            value += incr;
                        else
                            value -= incr;
                        resultData = Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
                        entry.Data = resultData;
                        return entry;
                    });

            resultDataOut = result ? resultData : null;
            return result;
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

                    int eventId = Interlocked.Increment(ref _currentSequenceId);
                    var newValue = new CacheEntry
                        {
                            CasUnique = casUnique,
                            Data = newData,
                            Expiry = exptime,
                            Flags = flags,
                            EventId = eventId,
                        };

                    bool updated = _cache.TryUpdate(key, newValue, entry);
                    // notify cas update
                    return updated;
                }
                return false;
            }
            CasStore(key, flags, exptime, casUnique, newData);
            return true;
        }

        public bool Replace(string key, ulong flags, DateTime exptime, byte[] data)
        {
            if (_cache.ContainsKey(key))
            {
                var existingLength = _cache[key].Data.Length;
                Interlocked.Add(ref _used, -existingLength);
                MakeSpaceForNewEntry(Math.Abs(data.Length - existingLength)); // In the case of replace this could be offset by the existing value
            }
            else
            {
                MakeSpaceForNewEntry(data.Length);
            }

            int eventId = Interlocked.Increment(ref _currentSequenceId);
            bool replaced = _cache.TryUpdate(key, e => new CacheEntry
                {
                    Data = data,
                    Expiry = exptime,
                    Flags = flags,
                    EventId = eventId
                });
            if (replaced)
            {
                Interlocked.Add(ref _used, data.Length);
                _notificationsSubject.OnNext(new StoreNotification
                    {
                        Key = key,
                        Data = data,
                        Expiry = exptime,
                        Flags = flags,
                        Operation = StoreOperation.Replace,
                        EventId = eventId
                    });
            }
            return replaced;
        }

        public bool Add(string key, ulong flags, DateTime exptime, byte[] data)
        {
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            int eventId = Interlocked.Increment(ref _currentSequenceId);
            var entry = new CacheEntry
                {
                    Data = data,
                    Expiry = exptime,
                    Flags = flags,
                    EventId = eventId
                };
            bool result = _cache.TryAdd(key, entry);

            if (result)
            {
                Interlocked.Add(ref _used, data.Length);
                _notificationsSubject.OnNext(new StoreNotification
                    {
                        Key = key,
                        Data = data,
                        Expiry = exptime,
                        Flags = flags,
                        Operation = StoreOperation.Add,
                        EventId = eventId
                    });
            }
            return result;
        }

        public bool Append(string key, ulong flags, DateTime exptime, byte[] data, bool prepend)
        {
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            int eventId = Interlocked.Increment(ref _currentSequenceId);
            bool exists = _cache.TryUpdate(key, e =>
                {
                    var newEntry = e;
                    newEntry.Data = prepend
                                        ? e.Data.Concat(data).ToArray()
                                        : data.Concat(e.Data).ToArray();
                    newEntry.EventId = eventId;
                    return newEntry;
                });
            if (exists)
            {

                Interlocked.Add(ref _used, data.Length);
                _notificationsSubject.OnNext(
                    new StoreNotification
                        {
                            Key = key,
                            Data = data,
                            Expiry = exptime,
                            Flags = flags,
                            Operation = prepend ? StoreOperation.Append : StoreOperation.Prepend,
                            EventId = eventId
                        });
            }
            return exists;
        }

        public bool Store(string key, ulong flags, byte[] data, DateTime exptime)
        {
            MakeSpaceForNewEntry(data.Length); // In the case of replace this could be offset by the existing value
            int eventId = Interlocked.Increment(ref _currentSequenceId);
            int size = 0;
            if (_cache.ContainsKey(key))
            {
                size = _cache[key].Data.Length;
            }
            _cache[key] = new CacheEntry
                {
                    Data = data,
                    Expiry = exptime,
                    Flags = flags,
                    EventId = eventId
                };

            Interlocked.Add(ref _used, data.Length - size);
            _notificationsSubject.OnNext(
                new StoreNotification
                    {
                        Key = key,
                        Data = data,
                        Expiry = exptime,
                        Flags = flags,
                        Operation = StoreOperation.Store,
                        EventId = eventId
                    });
            return true;
        }

        public bool Remove(string key)
        {
            CacheEntry entry;
            int eventId = Interlocked.Increment(ref _currentSequenceId);
            bool removed = _cache.TryRemove(key, out entry);
            if (removed)
            {
                Interlocked.Add(ref _used, -entry.Data.Length);
                _notificationsSubject.OnNext(new RemoveNotification { Key = key, EventId = eventId });
            }
            return removed;
        }

        public IEnumerable<KeyValuePair<string, CacheEntry>> Retrieve(IEnumerable<string> keys)
        {
            var tmp = keys.ToArray();
            var result = (from key in tmp
                          where _cache.ContainsKey(key)
                          select KeyValuePair.Create(key, _cache[key])).ToList();

            foreach (var key in tmp)
            {
                Use(key);
            }

            return result;
        }

        public IObservable<ICacheNotification> Notifications
        {
            get { return _notificationsSubject; }
        }

        public IEnumerable<string> Keys
        {
            get { return _cache.Keys; }
        }

        public Tuple<int, KeyValuePair<string, CacheEntry>[]> CurrentState
        {
            get
            {
                var contents = _cache.ToArray();
                var seq = Volatile.Read(ref _currentSequenceId);
                return Tuple.Create(seq, contents);
            }
        }
    
        public void MakeSpaceForNewEntry(int length)
        {
            while (!HasAvailableSpace((ulong) length))
            {
                _evictionStrategy.EvictEntry();
            }
        }

        private bool HasAvailableSpace(ulong length)
        {
            return Capacity >= Used + length;
        }

        private void CasStore(string key, ulong flags, DateTime exptime, ulong casUnique, byte[] newData)
        {
            MakeSpaceForNewEntry(newData.Length); // In the case of replace this could be offset by the existing value
            int eventId = Interlocked.Increment(ref _currentSequenceId);
            int size = 0;
            if (_cache.ContainsKey(key))
            {
                size = _cache[key].Data.Length;
            }
            _cache[key] = new CacheEntry
            {
                Data = newData,
                Expiry = exptime,
                Flags = flags,
                EventId = eventId,
                CasUnique = casUnique
            };
            Interlocked.Add(ref _used, newData.Length - size);
            _notificationsSubject.OnNext(
                new StoreNotification
                {
                    Key = key,
                    Data = newData,
                    Expiry = exptime,
                    Flags = flags,
                    Operation = StoreOperation.Store,
                    EventId = eventId
                });
        }

        public void Dispose()
        {
            _interval.Dispose();
            _notificationsSubject.Dispose();
            _evictionStrategy.Dispose();
        }

        public CacheEntry Get(string s)
        {
            Use(s);
            return _cache[s];
        }

        private void Use(string s)
        {
            _notificationsSubject.OnNext(new RetrieveNotification { EventId = 0, Key = s });
        }

        public bool TryGet(string s, out CacheEntry cacheEntry)
        {
            return _cache.TryGetValue(s, out cacheEntry);
        }
    }
}