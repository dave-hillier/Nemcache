using System.Collections.Generic;
using System.Linq;

namespace Nemcache.Service.Eviction
{
    internal class LRUEvictionStrategy : IEvictionStrategy, ICacheObserver
    {
        private readonly MemCache _cache;
        private readonly List<string> _keys = new List<string>();

        public LRUEvictionStrategy(MemCache cache)
        {
            _cache = cache;
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
    }
}