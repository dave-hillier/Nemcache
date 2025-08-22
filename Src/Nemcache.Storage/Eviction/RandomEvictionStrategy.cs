using System;
using System.Linq;

namespace Nemcache.Storage.Eviction
{
    public class RandomEvictionStrategy : IEvictionStrategy
    {
        private readonly IMemCache _cache;
        private readonly Random _rng;

        public RandomEvictionStrategy(IMemCache cache, Random rng = null)
        {
            _cache = cache;
            _rng = rng ?? new Random();
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

        public void Dispose()
        {
            
        }
    }
}