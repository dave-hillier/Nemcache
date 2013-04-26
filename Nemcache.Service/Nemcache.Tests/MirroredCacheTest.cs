using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using System.Text;

namespace Nemcache.Tests
{

    [TestClass]
    public class MirroredCacheTest
    {
        private MemCache _cache;
        private IMemCache _cacheCopy;

        [TestInitialize]
        public void Setup()
        {
            _cache = new MemCache(1000);
            _cacheCopy = new CacheCopy(new MemCache(1000), _cache.Notifications);
        }

        [TestMethod]
        public void AddedKeyCanBeRetrieved()
        {
            _cache.Add("key", 0, DateTime.MaxValue, Encoding.ASCII.GetBytes("Test"));

            var result = _cacheCopy.Retrieve(new[] { "key" }).ToArray();

            Assert.AreEqual("Test", Encoding.ASCII.GetString(result[0].Value.Data));
        }

        [TestMethod]
        public void LateSubscribe()
        {
            _cache.Add("key1", 0, DateTime.MaxValue, Encoding.ASCII.GetBytes("Test1"));
            _cacheCopy = new CacheCopy(new MemCache(1000), _cache.Notifications);

            _cache.Add("key2", 0, DateTime.MaxValue, Encoding.ASCII.GetBytes("Test2"));

            var result = _cacheCopy.Retrieve(new[] { "key1", "key2" }).ToArray();

            Assert.AreEqual("Test1", Encoding.ASCII.GetString(result[0].Value.Data));
            Assert.AreEqual("Test2", Encoding.ASCII.GetString(result[1].Value.Data));
        }
    }
}
