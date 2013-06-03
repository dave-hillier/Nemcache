﻿using System;
using System.Text;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using Nemcache.Service.Notifications;

namespace Nemcache.Tests
{
    [TestClass]
    public class MemcacheNotifcationsTest : ReactiveTest
    {
        private MemCache _cache;
        private ITestableObserver<ICacheNotification> _testObserver;
        private TestScheduler _testScheduler;

        [TestInitialize]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void Deleted()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("Some stuff in here..."), new DateTime(1999, 1, 1));
            _cache.Remove("key");

            var storeNotification = GetNotification();
            Assert.IsInstanceOfType(storeNotification, typeof (StoreNotification));
            var removeNotifaction = GetNotification(1);
            Assert.IsInstanceOfType(removeNotifaction, typeof (RemoveNotification));
        }

        [TestMethod]
        public void Append()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Append("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("12345"), false);

            Assert.AreEqual(2, _testObserver.Messages.Count);
            var storeNotification1 = GetNotification();
            Assert.IsInstanceOfType(storeNotification1, typeof (StoreNotification));
            var storeNotification2 = GetNotification();
            Assert.IsInstanceOfType(storeNotification2, typeof (StoreNotification));
        }

        [TestMethod]
        public void Replace()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Replace("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("12345"));

            Assert.AreEqual(2, _testObserver.Messages.Count);
            var storeNotification1 = GetNotification();
            Assert.IsInstanceOfType(storeNotification1, typeof (StoreNotification));
            var storeNotification2 = GetNotification(1);
            Assert.IsInstanceOfType(storeNotification2, typeof (StoreNotification));
        }

        [TestMethod]
        public void Clear()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Clear();

            Assert.AreEqual(2, _testObserver.Messages.Count);
            var storeNotification = GetNotification();
            Assert.IsInstanceOfType(storeNotification, typeof (StoreNotification));
            var clearNotification = GetNotification(1);
            Assert.IsInstanceOfType(clearNotification, typeof (ClearNotification));
        }

        [TestMethod]
        public void Touch()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Touch("key", new DateTime(1999, 1, 1));

            Assert.AreEqual(2, _testObserver.Messages.Count);
            var storeNotification1 = GetNotification();
            Assert.IsInstanceOfType(storeNotification1, typeof (StoreNotification));
            var storeNotification2 = GetNotification(1);
            Assert.IsInstanceOfType(storeNotification2, typeof (TouchNotification));
        }

        // TODO: Cas tests
        // TODO: mutate tests
    }
}