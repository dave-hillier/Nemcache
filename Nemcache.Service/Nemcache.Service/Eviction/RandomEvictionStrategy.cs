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
