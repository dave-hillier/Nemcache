using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests.Eviction
{
    [TestClass]
    public class LRUEvictionTests
    {
        private MemCache _cache;

        [TestInitialize]
        public void Setup()
        {
            _cache = new MemCache(10);
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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
    }
}