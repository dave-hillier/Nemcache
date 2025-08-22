using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Nemcache.Storage;

namespace Nemcache.Tests
{
    [TestFixture]
    public class MemcacheTest : ReactiveTest
    {
        private MemCache _cache;
        private TestScheduler _testScheduler;
        
        [SetUp]
        public void Setup()
        {
            _testScheduler = new TestScheduler();
            _cache = new MemCache(1000, _testScheduler);
        }

        [Test]
        public void UsedEmptyTest()
        {
            Assert.AreEqual(0UL, _cache.Used);
        }
        [Test]
        public void UsedSetTest()
        {
            _cache.Store("k", 0, new byte[10], DateTime.MaxValue);

            Assert.AreEqual(10UL, _cache.Used);
        }

        [Test]
        public void UsedRemoveTest()
        {
            _cache.Store("k", 0, new byte[10], DateTime.MaxValue);
            _cache.Remove("k");
            Assert.AreEqual(0UL, _cache.Used);
        }

        [Test]
        public void UsedReplacedTest()
        {
            _cache.Store("k", 0, new byte[999], DateTime.MaxValue);
            _cache.Store("k", 0, new byte[123], DateTime.MaxValue);

            Assert.AreEqual(123UL, _cache.Used);
        }

        [Test]
        public void UsedReducedAfterEvictTest()
        {
            _cache.Store("k1", 0, new byte[999], DateTime.MaxValue);
            _cache.Store("k2", 0, new byte[10], DateTime.MaxValue);

            Assert.AreEqual(10UL, _cache.Used);
        }

        [Test]
        public void EvictTwoTest()
        {
            _cache.Store("k1", 0, new byte[499], DateTime.MaxValue);
            _cache.Store("k2", 0, new byte[499], DateTime.MaxValue);
            _cache.Store("k3", 0, new byte[999], DateTime.MaxValue);

            Assert.AreEqual(999UL, _cache.Used);
        }


        [Test]
        public void UsedUpdateAfterExpireTest()
        {
            _cache.Store("k", 0, new byte[10], DateTime.MinValue + TimeSpan.FromSeconds(10));

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(11).Ticks);

            Assert.AreEqual(0UL, _cache.Used);
        }

        [Test]
        public void UsedRemainsAccurateUnderConcurrentStoreAndRemove()
        {
            Parallel.For(0, 100, i =>
            {
                var key = "k" + i;
                _cache.Store(key, 0, new byte[10], DateTime.MaxValue);
                _cache.Remove(key);
            });

            Assert.AreEqual(0UL, _cache.Used);
        }

        [Test]
        public void ImmediateExpiryExpiresItem()
        {
            _cache.Store("k", 0, new byte[10], DateTime.MinValue + TimeSpan.FromMilliseconds(1));
            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
            CacheEntry entry;
            var exists = _cache.TryGet("k", out entry);
            Assert.IsFalse(exists);
            Assert.AreEqual(0UL, _cache.Used);
        }

        [Test]
        public void AddReplaceAtomicWhenConcurrent()
        {
            int addSuccess = 0;
            Parallel.For(0, 10, i =>
            {
                if (_cache.Add("k", 0, DateTime.MaxValue, new byte[] { (byte)i }))
                {
                    Interlocked.Increment(ref addSuccess);
                }
            });
            Assert.AreEqual(1, addSuccess);
            Assert.AreEqual(1UL, _cache.Used);

            Parallel.For(0, 10, i =>
            {
                _cache.Replace("k", 0, DateTime.MaxValue, new byte[] { (byte)i });
            });
            Assert.AreEqual(1UL, _cache.Used);
            var value = _cache.Get("k").Data[0];
            Assert.That(value, Is.InRange(0, 9));
        }

        [Test]
        public void GetSingleItem()
        {
            _cache.Store("k", 0, Encoding.ASCII.GetBytes("foo"), DateTime.MinValue + TimeSpan.FromSeconds(10));

            Assert.AreEqual("foo", Encoding.ASCII.GetString(_cache.Get("k").Data));
        }

        [Test]
        public void GetMissing()
        {
            bool caught = false;
            try
            {
                _cache.Get("k");
            }
            catch (KeyNotFoundException )
            {
                caught = true;
            }
            Assert.IsTrue(caught);
        }

        [Test]
        public void TryGetFail()
        {
            CacheEntry cacheEntry;
            var result = _cache.TryGet("k", out cacheEntry);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void TryGet()
        {
            _cache.Store("k", 0, Encoding.ASCII.GetBytes("foo"), DateTime.MinValue + TimeSpan.FromSeconds(10));
            CacheEntry cacheEntry;
            var result = _cache.TryGet("k", out cacheEntry);
            Assert.AreEqual(true, result);
            Assert.AreEqual("foo", Encoding.ASCII.GetString(_cache.Get("k").Data));
        }
    }
}