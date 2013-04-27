using System;
using System.Text;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using Nemcache.Service.Notifications;

namespace Nemcache.Tests
{
    [TestClass]
    public class NotifcationsTest : ReactiveTest
    {
        private MemCache _cache;
        private TestScheduler _testScheduler;
        private ITestableObserver<ICacheNotification> _testObserver;

        [TestInitialize]
        public void Setup()
        {
            _cache = new MemCache(1000);
            _testScheduler = new TestScheduler();
        
            CreateObserverAndSubscribe();
        }

        [TestMethod]
        public void EnsureAddStoreNotification()
        {
            _cache.Add("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData"));

            var notification = GetFirstNotification();

            var store = notification as StoreNotification;
            Assert.AreEqual("key", store.Key);
            Assert.AreEqual("TestData", Encoding.ASCII.GetString(store.Data));
            Assert.AreEqual((ulong) 123, store.Flags);
            Assert.AreEqual(StoreOperation.Add, store.Operation);
            Assert.AreEqual(new DateTime(1999, 1, 1), store.Expiry);
        }

        [TestMethod]
        public void EnsureRemoveNotification()
        {
            _cache.Store("key1", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData"));
            _cache.Store("key2", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData"));

            _cache.Remove("key2");

            CreateObserverAndSubscribe();

            var notification = GetFirstNotification();
            var store = notification as StoreNotification;
            Assert.AreEqual("key1", store.Key);
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

        // TODO: gets add and remove when subscribed..
        // TODO: add and replace when subscribed...

        [TestMethod]
        public void EnsureStoreNotification()
        {
            _cache.Store("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData"));

            var notification = GetFirstNotification();
            var store = notification as StoreNotification;
            Assert.AreEqual("key", store.Key);
            Assert.AreEqual("TestData", Encoding.ASCII.GetString(store.Data));
            Assert.AreEqual((ulong) 123, store.Flags);
            Assert.AreEqual(StoreOperation.Store, store.Operation);
            Assert.AreEqual(new DateTime(1999, 1, 1), store.Expiry);
        }

        // TODO: don't notify if it wasnt added, as that has no effect.
        [TestMethod]
        public void DontReplayEntireHistory()
        {
            _cache.Add("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData"));
            _cache.Store("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData2"));

            CreateObserverAndSubscribe();

            var notification = GetFirstNotification();
            var store = notification as StoreNotification;
            Assert.AreEqual("TestData2", Encoding.ASCII.GetString(store.Data));
            Assert.AreEqual(StoreOperation.Add, store.Operation);
        }

        private ICacheNotification GetFirstNotification()
        {
            var notification = _testObserver.Messages[0].Value.Value;
            return notification;
        }
    }
}