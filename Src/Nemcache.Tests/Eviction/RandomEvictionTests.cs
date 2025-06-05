using System;
using System.Linq;
using System.Reactive.Concurrency;
using NUnit.Framework;
using Nemcache.Service;
using Nemcache.Service.Eviction;

namespace Nemcache.Tests.Eviction
{
    [TestFixture]
    public class RandomEvictionTests
    {
        [Test]
        public void EvictEmpty()
        {
            var memCache = new MemCache(100, Scheduler.Default); //Consider using mock
            var strategy = new RandomEvictionStrategy(memCache);
            strategy.EvictEntry();
            Assert.IsFalse(memCache.Keys.Any());
        }

        [Test]
        public void EvictSingle()
        {
            var memCache = new MemCache(100); //Consider using mock
            memCache.Add("mykey", 0, DateTime.MaxValue, new byte[] {});
            var strategy = new RandomEvictionStrategy(memCache);
            strategy.EvictEntry();
            Assert.AreEqual(0, memCache.Keys.Count());
        }

        [Test]
        public void Evict2()
        {
            var memCache = new MemCache(100); //Consider using mock
            memCache.Add("mykey1", 0, DateTime.MaxValue, new byte[] {});
            memCache.Add("mykey2", 0, DateTime.MaxValue, new byte[] {});
            var strategy = new RandomEvictionStrategy(memCache);
            strategy.EvictEntry();
            Assert.AreEqual(1, memCache.Keys.Count());
        }
    }
}