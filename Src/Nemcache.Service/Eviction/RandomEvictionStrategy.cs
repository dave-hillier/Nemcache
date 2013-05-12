using System;
using System.Linq;

namespace Nemcache.Service.Eviction
{
    internal class RandomEvictionStrategy : IEvictionStrategy
    {
        private readonly IMemCache _cache;
        private readonly Random _rng = new Random();

        public RandomEvictionStrategy(IMemCache cache)
        {
            _cache = cache;
        }

        public void EvictEntry()
        {
            var count = _cache.Keys.Count();
            if (count > 0)
            {
                var keyToEvict = _cache.Keys.ElementAt(_rng.Next(0, count));
                _cache.Remove(keyToEvict);
            }
        }
    }
}