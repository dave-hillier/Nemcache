using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nemcache.Service
{
    interface IEvictionStrategy
    {
        void MakeSpaceForNewEntry(int length);
    }

    internal class RandomEvictionStrategy : IEvictionStrategy
    {
        private readonly Random _rng = new Random();
        private MemCache _cache;

        public RandomEvictionStrategy(MemCache cache)
        {
            _cache = cache;
        }

        public void MakeSpaceForNewEntry(int length)
        {
            while (!HasAvailableSpace(length))
            {
                RemoveRandomEntry();
            }
        }

        private bool HasAvailableSpace(int length)
        {
            return _cache.Capacity >= _cache.Used + length;
        }

        private void RemoveRandomEntry()
        {
            var keyToEvict = _cache.Keys.ElementAt(_rng.Next(0, _cache.Keys.Count()));
            _cache.RemoveEntry(keyToEvict);
        }
    }
}
