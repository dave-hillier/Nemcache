using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    internal class LRUEvictionStrategy : IEvictionStrategy, ICacheObserver
    {
        private List<string> _keys = new List<string>();
        private MemCache _cache;

        public LRUEvictionStrategy(MemCache cache)
        {
            _cache = cache;
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
    }

}
