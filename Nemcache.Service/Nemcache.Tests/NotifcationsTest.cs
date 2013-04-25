using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace Nemcache.Tests
{
    [TestClass]
    public class NotifcationsTest // : ReactiveTest // TODO: get reactive test
    {
        private MemCache _cache;
        private ReplaySubject<ICacheNotification> _subject;
        [TestInitialize]
        public void Setup()
        {
            _cache = new MemCache(1000);
            _subject = new ReplaySubject<ICacheNotification>();
            _cache.Notifications.Subscribe(_subject);
        }

        [TestMethod]
        public void EnsureStoreNotification()
        {
            _cache.Add("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData"));

            var notification = _subject.First();

            var store = notification as Store;
            Assert.AreEqual("key", store.Key);
            Assert.AreEqual("TestData", Encoding.ASCII.GetString(store.Data));
            Assert.AreEqual((ulong)123, store.Flags);
            Assert.AreEqual(StoreOperation.Add, store.Operation);
            Assert.AreEqual(new DateTime(1999, 1, 1), store.Expiry);
        }

        // TODO: don't replay values with more up-to-date contents
        [TestMethod]
        public void DontReplayEntireHistory()
        {
            _cache.Add("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData"));
            _cache.Add("key", 123, new DateTime(1999, 1, 1), Encoding.ASCII.GetBytes("TestData2"));
            _subject = new ReplaySubject<ICacheNotification>();
            _cache.Notifications.Subscribe(_subject);

            var notification = _subject.Single();

            var store = notification as Store;

            Assert.AreEqual("TestData2", Encoding.ASCII.GetString(store.Data));
        }
    }
}
