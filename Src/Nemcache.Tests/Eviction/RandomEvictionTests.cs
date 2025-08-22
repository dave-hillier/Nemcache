using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reactive.Concurrency;
using NUnit.Framework;
using Nemcache.Storage;
using Nemcache.Storage.Eviction;

namespace Nemcache.Tests.Eviction
{
    [TestFixture]
    public class RandomEvictionTests
    {
        private static RandomEvictionStrategy CreateStrategy(MemCache cache, int seed = 123)
        {
            var strategy = new RandomEvictionStrategy(cache);
            typeof(RandomEvictionStrategy)
                .GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(strategy, new Random(seed));
            return strategy;
        }

        [Test]
        public void EvictEmpty()
        {
            var memCache = new MemCache(100, Scheduler.Default);
            var strategy = CreateStrategy(memCache);
            strategy.EvictEntry();
            Assert.IsFalse(memCache.Keys.Any());
        }

        [Test]
        public void EvictSingle()
        {
            var memCache = new MemCache(100);
            memCache.Add("mykey", 0, DateTime.MaxValue, new byte[] { });
            var strategy = CreateStrategy(memCache);
            strategy.EvictEntry();
            Assert.AreEqual(0, memCache.Keys.Count());
        }

        [Test]
        public void Evict2()
        {
            var memCache = new MemCache(100);
            memCache.Add("mykey1", 0, DateTime.MaxValue, new byte[] { });
            memCache.Add("mykey2", 0, DateTime.MaxValue, new byte[] { });
            var strategy = CreateStrategy(memCache);
            strategy.EvictEntry();
            Assert.AreEqual(1, memCache.Keys.Count());
        }

        [Test]
        public void EvictRepeatedlyRemovesAllOriginalKeysInDeterministicOrder()
        {
            var memCache = new MemCache(100);
            var keys = new[] { "a", "b", "c", "d", "e" };
            foreach (var key in keys)
            {
                memCache.Add(key, 0, DateTime.MaxValue, new byte[] { });
            }

            var strategy = CreateStrategy(memCache, seed: 42);
            var expectedOrder = new[] { "d", "a", "b", "e", "c" };
            var removed = new List<string>();

            for (var i = 0; i < keys.Length; i++)
            {
                var before = memCache.Keys.OrderBy(k => k).ToList();
                strategy.EvictEntry();
                var after = memCache.Keys.OrderBy(k => k).ToList();

                var removedKey = before.Except(after).Single();
                removed.Add(removedKey);
            }

            CollectionAssert.AreEqual(expectedOrder, removed);
            Assert.IsFalse(memCache.Keys.Any());
        }
    }
}
