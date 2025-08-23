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
            // Materialize and sort to ensure deterministic order, which allows
            // tests to reliably predict the eviction sequence. The overhead is
            // acceptable as eviction occurs infrequently and caches typically
            // contain a limited number of keys.
            var keys = _cache.Keys.OrderBy(k => k).ToList();
            var count = keys.Count;
            if (count > 0)
            {
                var keyToEvict = keys[_rng.Next(0, count)];
                _cache.Remove(keyToEvict);
            }
        }

        public void Dispose()
        {
            
        }
    }
}