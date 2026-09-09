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
            Assert.That(store, Is.Not.Null);
            Assert.That(store.Key, Is.EqualTo("key"));
            Assert.That(Encoding.ASCII.GetString(store.Data), Is.EqualTo("TestData"));
            Assert.That(store.Flags, Is.EqualTo((ulong) 123));
            Assert.That(store.Operation, Is.EqualTo(StoreOperation.Add));
            Assert.That(store.Expiry, Is.EqualTo(new DateTime(1999, 1, 1)));
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
            Assert.That(store, Is.Not.Null);
            Assert.That(store.Key, Is.EqualTo("key"));
            Assert.That(Encoding.ASCII.GetString(store.Data), Is.EqualTo("TestData"));
            Assert.That(store.Flags, Is.EqualTo((ulong) 123));
            Assert.That(store.Operation, Is.EqualTo(StoreOperation.Store));
            Assert.That(store.Expiry, Is.EqualTo(new DateTime(1999, 1, 1)));
        }

        [Test]
        public void Deleted()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("Some stuff in here..."), new DateTime(1999, 1, 1));
            _cache.Remove("key");

            var storeNotification = GetNotification();
            Assert.That(storeNotification, Is.InstanceOf<StoreNotification>());
            var removeNotifaction = GetNotification(1);
            Assert.That(removeNotifaction, Is.InstanceOf<RemoveNotification>());
        }

        [Test]
        public void Append()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Append("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("12345"), false);

            Assert.That(_testObserver.Messages.Count, Is.EqualTo(2));
            var storeNotification1 = GetNotification();
            Assert.That(storeNotification1, Is.InstanceOf<StoreNotification>());
            var storeNotification2 = GetNotification();
            Assert.That(storeNotification2, Is.InstanceOf<StoreNotification>());
        }

        [Test]
        public void Replace()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Replace("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("12345"));

            Assert.That(_testObserver.Messages.Count, Is.EqualTo(2));
            var storeNotification1 = GetNotification();
            Assert.That(storeNotification1, Is.InstanceOf<StoreNotification>());
            var storeNotification2 = GetNotification(1);
            Assert.That(storeNotification2, Is.InstanceOf<StoreNotification>());
        }

        [Test]
        public void Clear()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Clear();

            Assert.That(_testObserver.Messages.Count, Is.EqualTo(2));
            var storeNotification = GetNotification();
            Assert.That(storeNotification, Is.InstanceOf<StoreNotification>());
            var clearNotification = GetNotification(1);
            Assert.That(clearNotification, Is.InstanceOf<ClearNotification>());
        }

        [Test]
        public void Touch()
        {
            _cache.Store("key", 123, Encoding.ASCII.GetBytes("12345"), new DateTime(1999, 1, 1));
            _cache.Touch("key", new DateTime(1999, 1, 1));

            Assert.That(_testObserver.Messages.Count, Is.EqualTo(2));
            var storeNotification1 = GetNotification();
            Assert.That(storeNotification1, Is.InstanceOf<StoreNotification>());
            var storeNotification2 = GetNotification(1);
            Assert.That(storeNotification2, Is.InstanceOf<TouchNotification>());
        }

        // TODO: Cas tests
        // TODO: mutate tests
    }
}
