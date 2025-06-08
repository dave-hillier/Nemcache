using System;
using System.Text;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Nemcache.Storage;
using Nemcache.Storage.Notifications;

namespace Nemcache.Tests
{
    [TestFixture]
    public class MemcacheNotifcationsTest : ReactiveTest
    {
        private MemCache _cache;
        private ITestableObserver<ICacheNotification> _testObserver;
        private TestScheduler _testScheduler;

        [SetUp]
        public void Setup()
        {
            _cache = new MemCache(1000);
            _testScheduler = new TestScheduler();
            CreateObserverAndSubscribe();
        }

        private ICacheNotification GetNotification(int index = 0)
        {
            var notification = _testObserver.Messages[index].Value.Value;
            return notification;
        }

        [Test]
        public void EnsureAddStoreNotification()
        {
            _cache.Add("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData"));

            var notification = GetNotification();

            var store = notification as StoreNotification;
            Assert.IsNotNull(store);
            Assert.AreEqual("key", store.Key);
            Assert.AreEqual("TestData", Encoding.ASCII.GetString(store.Data));
            Assert.AreEqual((ulong) 123, store.Flags);
            Assert.AreEqual(StoreOperation.Add, store.Operation);
            Assert.AreEqual(new DateTime(1999, 1, 1), store.Expiry);
        }

        private void CreateObserverAndSubscribe()
        {
            _testObserver = _testScheduler.CreateObserver<ICacheNotification>();
            _cache.Notifications.Subscribe(_testObserver);
        }

        [Test]
        public void EnsureStoreNotification()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("TestData"), new DateTime(1999, 1, 1));

            var notification = GetNotification();
            var store = notification as StoreNotification;
            Assert.IsNotNull(store);
            Assert.AreEqual("key", store.Key);
            Assert.AreEqual("TestData", Encoding.ASCII.GetString(store.Data));
            Assert.AreEqual((ulong) 123, store.Flags);
            Assert.AreEqual(StoreOperation.Store, store.Operation);
            Assert.AreEqual(new DateTime(1999, 1, 1), store.Expiry);
        }

        [Test]
        public void Deleted()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("Some stuff in here..."), new DateTime(1999, 1, 1));
            _cache.Remove("key");

            var storeNotification = GetNotification();
            Assert.IsInstanceOf<StoreNotification>(storeNotification);
            var removeNotifaction = GetNotification(1);
            Assert.IsInstanceOf<RemoveNotification>(removeNotifaction);
        }

        [Test]
        public void Append()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Append("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("12345"), false);

            Assert.AreEqual(2, _testObserver.Messages.Count);
            var storeNotification1 = GetNotification();
            Assert.IsInstanceOf<StoreNotification>(storeNotification1);
            var storeNotification2 = GetNotification();
            Assert.IsInstanceOf<StoreNotification>(storeNotification2);
        }

        [Test]
        public void Replace()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Replace("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("12345"));

            Assert.AreEqual(2, _testObserver.Messages.Count);
            var storeNotification1 = GetNotification();
            Assert.IsInstanceOf<StoreNotification>(storeNotification1);
            var storeNotification2 = GetNotification(1);
            Assert.IsInstanceOf<StoreNotification>(storeNotification2);
        }

        [Test]
        public void Clear()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Clear();

            Assert.AreEqual(2, _testObserver.Messages.Count);
            var storeNotification = GetNotification();
            Assert.IsInstanceOf<StoreNotification>(storeNotification);
            var clearNotification = GetNotification(1);
            Assert.IsInstanceOf<ClearNotification>(clearNotification);
        }

        [Test]
        public void Touch()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Touch("key", new DateTime(1999, 1, 1));

            Assert.AreEqual(2, _testObserver.Messages.Count);
            var storeNotification1 = GetNotification();
            Assert.IsInstanceOf<StoreNotification>(storeNotification1);
            var storeNotification2 = GetNotification(1);
            Assert.IsInstanceOf<TouchNotification>(storeNotification2);
        }

        // TODO: Cas tests
        // TODO: mutate tests
    }
}
