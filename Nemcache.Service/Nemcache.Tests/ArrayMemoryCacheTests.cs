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


        [TestMethod]
        public void TestIncr()
        {
            var cache = new ArrayMemoryCache();

            // Put something in the cache
            var value = Encoding.ASCII.GetBytes("123");
            cache.Storage.Set("Key", value, DateTimeOffset.Now + TimeSpan.FromDays(1));

            var result = cache.Increase("Key", 123);

            Assert.AreEqual("246\r\n", Encoding.ASCII.GetString(result));
        }

        [TestMethod]
        public void TestIncrNotFound()
        {
            var cache = new ArrayMemoryCache();

            // Put something in the cache
            var value = Encoding.ASCII.GetBytes("123");
            cache.Storage.Set("Key", value, DateTimeOffset.Now + TimeSpan.FromDays(1));

            var result = cache.Increase("KeyOOOOO", 123);

            Assert.AreEqual("NOT_FOUND\r\n", Encoding.ASCII.GetString(result));
        }
        [TestMethod]
        public void TestDecr()
        {
            var cache = new ArrayMemoryCache();

            // Put something in the cache
            var value = Encoding.ASCII.GetBytes("100");
            cache.Storage.Set("Key", value, DateTimeOffset.Now + TimeSpan.FromDays(1));

            ulong decrement = 11;
            var result = cache.Decrease("Key", decrement);

            Assert.AreEqual("89\r\n", Encoding.ASCII.GetString(result));
        }

        // TODO: max ulong tests
        [TestMethod]
        public void TestDecrNotFound()
        {
            var cache = new ArrayMemoryCache();

            // Put something in the cache
            var value = Encoding.ASCII.GetBytes("100");
            cache.Storage.Set("Key", value, DateTimeOffset.Now + TimeSpan.FromDays(1));

            ulong decrement = 11;
            var result = cache.Decrease("RandomKey", decrement);

            Assert.AreEqual("NOT_FOUND\r\n", Encoding.ASCII.GetString(result));
        }

        // TODO: max ulong tests
        [TestMethod]
        public void TestDecrNot64Bit()
        {
            var cache = new ArrayMemoryCache();

            // Put something in the cache
            var value = Encoding.ASCII.GetBytes("Foo");
            cache.Storage.Set("Key", value, DateTimeOffset.Now + TimeSpan.FromDays(1));

            ulong decrement = 11;
            var result = cache.Decrease("Key", decrement);

            Assert.AreEqual("ERROR Key does not represent a 64-bit unsigned int\r\n", Encoding.ASCII.GetString(result));
        }
    }
}