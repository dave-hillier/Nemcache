using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using NUnit.Framework;
using Nemcache.Storage;
using Nemcache.Storage.Eviction;

namespace Nemcache.Tests.Eviction
{
    [TestFixture]
    public class RandomEvictionTests
    {
        [Test]
        public void EvictEmpty()
        {
            var memCache = new MemCache(100, Scheduler.Default);
            var strategy = new RandomEvictionStrategy(memCache, new Random());
            strategy.EvictEntry();
            Assert.IsFalse(memCache.Keys.Any());
        }

        [Test]
        public void EvictSingle()
        {
            var memCache = new MemCache(100);
            memCache.Add("mykey", 0, DateTime.MaxValue, new byte[] { });
            var strategy = new RandomEvictionStrategy(memCache, new Random());
            strategy.EvictEntry();
            Assert.AreEqual(0, memCache.Keys.Count());
        }

        [Test]
        public void Evict2()
        {
            var memCache = new MemCache(100);
            memCache.Add("mykey1", 0, DateTime.MaxValue, new byte[] { });
            memCache.Add("mykey2", 0, DateTime.MaxValue, new byte[] { });
            var strategy = new RandomEvictionStrategy(memCache, new Random());
            strategy.EvictEntry();
            Assert.AreEqual(1, memCache.Keys.Count());
        }

        [Test]
        public void EvictRepeatedlyRemovesAllOriginalKeys()
        {
            var memCache = new MemCache(100);
            var keys = new[] { "a", "b", "c", "d", "e" };
            foreach (var key in keys)
            {
                memCache.Add(key, 0, DateTime.MaxValue, new byte[] { });
            }

            var strategy = new RandomEvictionStrategy(memCache, new Random(42));
            var remaining = new HashSet<string>(keys);
            var removed = new HashSet<string>();

            for (var i = 0; i < keys.Length; i++)
            {
                var before = memCache.Keys.ToList();
                strategy.EvictEntry();
                var after = memCache.Keys.ToList();

                var removedKey = before.Except(after).Single();
                Assert.IsTrue(remaining.Contains(removedKey));
                remaining.Remove(removedKey);
                removed.Add(removedKey);

                CollectionAssert.AreEquivalent(remaining, after);
            }

            CollectionAssert.AreEquivalent(keys, removed);
            Assert.IsFalse(memCache.Keys.Any());
        }

        [Test]
        public void EvictionOrderIsDeterministicWithFixedSeed()
        {
            var seed = 42;
            var keys = new[] { "a", "b", "c", "d", "e" };

            var cache1 = new MemCache(100);
            var cache2 = new MemCache(100);

            foreach (var key in keys)
            {
                cache1.Add(key, 0, DateTime.MaxValue, new byte[] { });
                cache2.Add(key, 0, DateTime.MaxValue, new byte[] { });
            }

            var strategy1 = new RandomEvictionStrategy(cache1, new Random(seed));
            var strategy2 = new RandomEvictionStrategy(cache2, new Random(seed));

            var order1 = new List<string>();
            var order2 = new List<string>();

            for (var i = 0; i < keys.Length; i++)
            {
                var before1 = cache1.Keys.ToList();
                strategy1.EvictEntry();
                var after1 = cache1.Keys.ToList();
                order1.Add(before1.Except(after1).Single());

                var before2 = cache2.Keys.ToList();
                strategy2.EvictEntry();
                var after2 = cache2.Keys.ToList();
                order2.Add(before2.Except(after2).Single());
            }

            CollectionAssert.AreEqual(order1, order2);
        }
    }
}
