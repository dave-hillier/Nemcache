using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nemcache.Storage;

namespace Nemcache.Tests.Eviction
{
    [TestFixture]
    public class LRUEvictionTests
    {
        private MemCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new MemCache(10);
        }

        [Test]
        public void EvictsEarliestAdded()
        {
            _cache.Add("key1", 0, DateTime.MaxValue, new byte[] {0, 1, 2, 3, 4});
            _cache.Add("key2", 0, DateTime.MaxValue, new byte[] {0, 1, 2, 3, 4});
            _cache.Add("key3", 0, DateTime.MaxValue, new byte[] {0, 1, 2, 3, 4});

            var keys = _cache.Keys.ToArray();

            Assert.AreEqual(2, keys.Length);
            Assert.IsTrue(keys.Contains("key2"));
            Assert.IsTrue(keys.Contains("key3"));
        }

        [Test]
        public void EvictsEarliestStored()
        {
            _cache.Store("key1", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);
            _cache.Store("key2", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);
            _cache.Store("key3", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);

            var keys = _cache.Keys.ToArray();

            Assert.AreEqual(2, keys.Length);
            Assert.IsTrue(keys.Contains("key2"));
            Assert.IsTrue(keys.Contains("key3"));
        }

        [Test]
        public void ReplacePreventsEvict()
        {
            _cache.Store("key1", 0, new byte[] {0, 1, 2, 3}, DateTime.MaxValue);
            _cache.Store("key2", 0, new byte[] {0, 1, 2, 3}, DateTime.MaxValue);
            _cache.Replace("key1", 0, DateTime.MaxValue, new byte[] {3, 2, 1, 0});

            _cache.Store("key3", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);

            var keys = _cache.Keys.ToArray();

            Assert.AreEqual(2, keys.Length);
            Assert.IsTrue(keys.Contains("key1"));
            Assert.IsTrue(keys.Contains("key3"));
        }

        [Test]
        public void TouchPreventsEvict()
        {
            _cache.Store("key1", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);
            _cache.Store("key2", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);
            _cache.Touch("key1", DateTime.MaxValue);

            _cache.Store("key3", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);

            var keys = _cache.Keys.ToArray();

            Assert.AreEqual(2, keys.Length);
            Assert.IsTrue(keys.Contains("key1"));
            Assert.IsTrue(keys.Contains("key3"));
        }

        [Test]
        public void RetrievePreventsEvict()
        {
            _cache.Store("key1", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);
            _cache.Store("key2", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);
            _cache.Retrieve(new[] {"key1"});

            _cache.Store("key3", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);

            var keys = _cache.Keys.ToArray();

            Assert.AreEqual(2, keys.Length);
            Assert.IsTrue(keys.Contains("key1"));
            Assert.IsTrue(keys.Contains("key3"));
        }

        [Test]
        public void DoesntTryToEvictRemoved()
        {
            _cache.Add("key1", 0, DateTime.MaxValue, new byte[] {0, 1, 2, 3, 4});
            _cache.Add("key2", 0, DateTime.MaxValue, new byte[] {0, 1, 2, 3, 4});
            _cache.Remove("key1");
            _cache.Add("key3", 0, DateTime.MaxValue, new byte[] {0, 1, 2, 3, 4});
            _cache.Add("key4", 0, DateTime.MaxValue, new byte[] {0, 1, 2, 3, 4});

            var keys = _cache.Keys.ToArray();

            Assert.AreEqual(2, keys.Length);
            Assert.IsTrue(keys.Contains("key3"));
            Assert.IsTrue(keys.Contains("key4"));
        }

        [Test]
        public void MultipleEvictionsLeaveMostRecentKeys()
        {
            for (int i = 0; i < 5; i++)
            {
                _cache.Store($"key{i}", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);
            }

            var keys = _cache.Keys.ToArray();

            Assert.AreEqual(2, keys.Length);
            Assert.IsTrue(keys.Contains("key3"));
            Assert.IsTrue(keys.Contains("key4"));
        }

        [Test]
        public void ZeroCapacityStoresNothing()
        {
            var cache = new MemCache(0);
            cache.Store("key1", 0, new byte[] {0}, DateTime.MaxValue);

            Assert.IsFalse(cache.Keys.Any());
        }

        [Test]
        public void CapacityOneEvictsOlderEntries()
        {
            var cache = new MemCache(1);
            cache.Store("key1", 0, new byte[] {0}, DateTime.MaxValue);
            cache.Store("key2", 0, new byte[] {0}, DateTime.MaxValue);

            var keys = cache.Keys.ToArray();

            Assert.AreEqual(1, keys.Length);
            Assert.IsTrue(keys.Contains("key2"));
        }

        [Test]
        public void ConcurrentStoreRetrieveMaintainsOrder()
        {
            _cache.Store("key1", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);
            _cache.Store("key2", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);

            Parallel.For(0, 20, i =>
            {
                _cache.Retrieve(new[] {"key1"});
                _cache.Store($"key{i + 3}", 0, new byte[] {0, 1, 2, 3, 4}, DateTime.MaxValue);
            });

            var keys = _cache.Keys.ToArray();

            Assert.AreEqual(2, keys.Length);
            Assert.IsFalse(keys.Contains("key2"));
        }
    }
}