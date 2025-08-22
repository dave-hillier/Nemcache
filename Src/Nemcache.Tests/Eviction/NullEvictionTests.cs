using System;
using NUnit.Framework;
using Nemcache.Storage;
using Nemcache.Storage.Eviction;

namespace Nemcache.Tests.Eviction
{
    [TestFixture]
    public class NullEvictionTests
    {
        [Test]
        public void EvictDoesNothing()
        {
            var cache = new MemCache(10);
            cache.Store("key", 0, new byte[] {1}, DateTime.MaxValue);
            var usedBefore = cache.Used;

            var strategy = new NullEvictionStrategy();
            strategy.EvictEntry();

            CacheEntry entry;
            Assert.IsTrue(cache.TryGet("key", out entry));
            Assert.AreEqual(usedBefore, cache.Used);
        }
    }
}
