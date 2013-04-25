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
    class NotifcationsTest // : ReactiveTest // TODO: get reactive test
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
            _cache.Add("key", 0, DateTime.MaxValue, Encoding.ASCII.GetBytes("TestData"));

            var notification = _subject.Take(1).First();

            var store = notification as Store;
            Assert.AreEqual("key", store.Key);
            Assert.AreEqual("TestData", Encoding.ASCII.GetString(store.Data));
            // TODO: Type of store op
            // TODO: flags
            // TODO: expiry
        }
    }
}
