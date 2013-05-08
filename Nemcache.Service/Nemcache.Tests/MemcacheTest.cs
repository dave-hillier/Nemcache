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
            _testScheduler = new TestScheduler();
            _cache = new MemCache(1000, _testScheduler);
        }

        [TestMethod]
        public void UsedEmptyTest()
        {
            Assert.AreEqual(0UL, _cache.Used);
        }
        [TestMethod]
        public void UsedSetTest()
        {
            _cache.Store("k", 0, new byte[10], DateTime.MaxValue);

            Assert.AreEqual(10UL, _cache.Used);
        }

        [TestMethod]
        public void UsedRemoveTest()
        {
            _cache.Store("k", 0, new byte[10], DateTime.MaxValue);
            _cache.Remove("k");
            Assert.AreEqual(0UL, _cache.Used);
        }

        [TestMethod]
        public void UsedReplacedTest()
        {
            _cache.Store("k", 0, new byte[999], DateTime.MaxValue);
            _cache.Store("k", 0, new byte[123], DateTime.MaxValue);

            Assert.AreEqual(123UL, _cache.Used);
        }

        [TestMethod]
        public void UsedReducedAfterEvictTest()
        {
            _cache.Store("k1", 0, new byte[999], DateTime.MaxValue);
            _cache.Store("k2", 0, new byte[10], DateTime.MaxValue);

            Assert.AreEqual(10UL, _cache.Used);
        }

        [TestMethod]
        public void EvictTwoTest()
        {
            _cache.Store("k1", 0, new byte[499], DateTime.MaxValue);
            _cache.Store("k2", 0, new byte[499], DateTime.MaxValue);
            _cache.Store("k3", 0, new byte[999], DateTime.MaxValue);

            Assert.AreEqual(999UL, _cache.Used);
        }


        [TestMethod]
        public void UsedUpdateAfterExpireTest()
        {
            _cache.Store("k", 0, new byte[10], DateTime.MinValue + TimeSpan.FromSeconds(10));

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(11).Ticks);

            Assert.AreEqual(0UL, _cache.Used);
        }
    }
}