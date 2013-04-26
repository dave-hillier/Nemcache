using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nemcache.Service
{
    interface IEvictionStrategy
    {
        void EvictEntry();
    }

    interface ICacheObserver
    {
        void Use(string key);
        void Remove(string key);
    }

    internal class NullEvictionStrategy : IEvictionStrategy
    {
        public void EvictEntry()
        {
        }
    }

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
            lock(_keys)
            {
                _keys.Remove(key);
            }
        }
    }

    internal class RandomEvictionStrategy : IEvictionStrategy
    {
        private readonly Random _rng = new Random();
        private MemCache _cache;

        public RandomEvictionStrategy(MemCache cache)
        {
            _cache = cache;
        }

        public void EvictEntry()
        {
            var keyToEvict = _cache.Keys.ElementAt(_rng.Next(0, _cache.Keys.Count()));
            _cache.Remove(keyToEvict);
        }
    }
}
