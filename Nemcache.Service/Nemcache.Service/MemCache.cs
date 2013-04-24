using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nemcache.Service
{
    internal class MemCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        private readonly Random _rng = new Random(); // TODO: extract an expiry strategy

        public MemCache(int capacity)
        {
            Capacity = capacity;
        }

        internal struct CacheEntry
        {
            public ulong Flags { get; set; }
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


        public void Flush()
        {
            _cache.Clear();
        }

        public TimeSpan ScheduleFlush(TimeSpan delay)
        {
            Scheduler.Current.Schedule(delay, () => _cache.Clear());
            return delay;
        }

        public bool Touch(string key, DateTime exptime)
        {
            bool success = false;
            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                CacheEntry touched = entry;
                touched.Expiry = exptime;
                _cache.TryUpdate(key, touched, entry); // OK to fail if something else has updated?
                success = true;
            }
            return success;
        }

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

        // TODO: remove the code returns
        public bool Cas(string key, ulong flags, DateTime exptime, ulong casUnique, byte[] data)
        {
            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                if (entry.CasUnique == casUnique)
                {
                    var spaceRequired = Math.Abs(data.Length - entry.Data.Length);
                    if (spaceRequired > 0)
                        MakeSpaceForData(spaceRequired);
                    var newValue = new CacheEntry { CasUnique = casUnique, Data = data, Expiry = exptime, Flags = flags };
                    var stored = _cache.TryUpdate(key, newValue, entry);
                    return stored;
                }
                else
                {
                    return false;
                }
            }

            _cache[key] = new CacheEntry { CasUnique = casUnique, Data = data, Expiry = exptime, Flags = flags };
            return true;
        }



        public bool Replace(string key, ulong flags, DateTime exptime, byte[] data)
        {
            MakeSpaceForData(data.Length); // In the case of replace this could be offset by the existing value
            var exists = _cache.TryUpdate(key, e =>
            {
                return new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
            });
            return exists;
        }

        public bool Add(string key, ulong flags, DateTime exptime, byte[] data)
        {
            MakeSpaceForData(data.Length); // In the case of replace this could be offset by the existing value
            var entry = new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
            var result = _cache.TryAdd(key, entry);
            return result;
        }

        public bool Append(string key, ulong flags, DateTime exptime, byte[] data, bool prepend)
        {
            MakeSpaceForData(data.Length); // In the case of replace this could be offset by the existing value
            var exists = _cache.TryUpdate(key, e =>
            {
                var newEntry = e;
                newEntry.Data = prepend ?
                    e.Data.Concat(data).ToArray() :
                    data.Concat(e.Data).ToArray();
                return newEntry;
            });
            return exists ? true : Store(key, flags, exptime, data);
        }

        public bool Store(string key, ulong flags, DateTime exptime, byte[] data)
        {
            MakeSpaceForData(data.Length); // In the case of replace this could be offset by the existing value
            _cache[key] = new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
            return true;
        }

        public bool Delete(string key)
        {
            CacheEntry entry;
            return _cache.TryRemove(key, out entry);
        }

        private void MakeSpaceForData(int length)
        {
            while (Used + length > Capacity)
            {
                RemoveRandomEntry();
            }
        }

        private void RemoveRandomEntry()
        {
            var keyToEvict = _cache.Keys.ElementAt(_rng.Next(0, _cache.Keys.Count));
            CacheEntry entry;
            _cache.TryRemove(keyToEvict, out entry);
        }

        public IEnumerable<KeyValuePair<string, CacheEntry>> Retrieve(IEnumerable<string> keys)
        {
            return from key in keys
                   where _cache.ContainsKey(key)
                   select KeyValuePair.Create(key, _cache[key]);
        }

    }

}
