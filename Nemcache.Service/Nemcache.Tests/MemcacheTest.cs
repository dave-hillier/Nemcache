using System;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class MemcacheTest : ReactiveTest
    {
        private MemCache _cache;
        private TestScheduler _testScheduler;
        
        [TestInitialize]
        public void Setup()
        {
            _cache = new MemCache(1000);
            _testScheduler = new TestScheduler();
        }


        [TestMethod]
        public void UsedEmptyTest()
        {
            Assert.AreEqual(0UL, _cache.Used);
        }
        [TestMethod]
        public void UsedSetTest()
        {
            _cache.Store("k", 0, DateTime.MaxValue, new byte[10]);

            Assert.AreEqual(10UL, _cache.Used);
        }

        [TestMethod]
        public void UsedRemoveTest()
        {
            _cache.Store("k", 0, DateTime.MaxValue, new byte[10]);
            _cache.Remove("k");
            Assert.AreEqual(0UL, _cache.Used);
        }

        [TestMethod]
        public void UsedReplacedTest()
        {
            _cache.Store("k", 0, DateTime.MaxValue, new byte[999]);
            _cache.Store("k", 0, DateTime.MaxValue, new byte[123]);

            Assert.AreEqual(123UL, _cache.Used);
        }

        [TestMethod]
        public void UsedReducedAfterEvictTest()
        {
            _cache.Store("k1", 0, DateTime.MaxValue, new byte[999]);
            _cache.Store("k2", 0, DateTime.MaxValue, new byte[10]);

            Assert.AreEqual(10UL, _cache.Used);
        }

        [TestMethod]
        public void EvictTwoTest()
        {
            _cache.Store("k1", 0, DateTime.MaxValue, new byte[499]);
            _cache.Store("k2", 0, DateTime.MaxValue, new byte[499]);
            _cache.Store("k3", 0, DateTime.MaxValue, new byte[999]);

            Assert.AreEqual(999UL, _cache.Used);
        }

    }
}