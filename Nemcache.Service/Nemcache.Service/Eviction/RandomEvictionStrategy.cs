using System;
using System.Linq;

namespace Nemcache.Service.Eviction
{
    internal class RandomEvictionStrategy : IEvictionStrategy
    {
        private readonly MemCache _cache;
        private readonly Random _rng = new Random();

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