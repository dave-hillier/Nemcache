using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class ArrayMemoryCacheTests
    {
        [TestMethod]
        public void TestSet()
        {
            var cache = new ArrayMemoryCache();
            var value = Encoding.ASCII.GetBytes("Value");

            cache.Set("TheKey", value);

            var result = (byte[])cache.Storage.Get("TheKey");

            Assert.IsTrue(value.SequenceEqual(result));
        }

        [TestMethod]
        public void TestUpdate()
        {
            var cache = new ArrayMemoryCache();
            var updatedValue = Encoding.ASCII.GetBytes("NewValue");

            cache.Storage.Set("TheKey", Encoding.ASCII.GetBytes("OriginalValue"), DateTimeOffset.Now+TimeSpan.FromDays(1));
            
            cache.Set("TheKey", updatedValue);

            var result = (byte[])cache.Storage.Get("TheKey");

            Assert.IsTrue(updatedValue.SequenceEqual(result));
        }

        [TestMethod]
        public void TestGetMissing()
        {
            var cache = new ArrayMemoryCache();

            var result = cache.Get("NothingStoredUnderThisKey");

            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod]
        public void TestGet()
        {
            var cache = new ArrayMemoryCache();

            var value = Encoding.ASCII.GetBytes("Value");
            cache.Storage.Set("TheKey", value, DateTimeOffset.Now + TimeSpan.FromDays(1));

            var result = (byte[])cache.Storage.Get("TheKey");

            Assert.IsTrue(value.SequenceEqual(result));
        }

        [TestMethod]
        public void TestDelete()
        {
            var cache = new ArrayMemoryCache();

            // Put something in the cache
            var value = Encoding.ASCII.GetBytes("Value");
            cache.Storage.Set("TheKeyToBeDeleted", value, DateTimeOffset.Now + TimeSpan.FromDays(1));

            // Remove it
            cache.Remove("TheKeyToBeDeleted");

            // test for presence
            var result = cache.Get("TheKeyToBeDeleted");

            Assert.IsTrue(result.Length == 0);
        }
    }
}