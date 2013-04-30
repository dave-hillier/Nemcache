using System.Globalization;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using Nemcache.Service.Notifications;
using System.Reactive;
using System.Reactive.Linq;

namespace Nemcache.Tests
{
    [TestClass]
    public class CombineCurrentStateWithUpdatesTests : ReactiveTest
    {
        class DummyNotification : ICacheNotification
        {
            public int EventId { get; set; }
            public override string ToString()
            {
                return "Notification: " + EventId.ToString(CultureInfo.InvariantCulture);
            }
        }

        [TestMethod]
        public void DummyToString()
        {
            var s = new DummyNotification() {EventId = 123}.ToString();
            Assert.AreEqual("Notification: 123", s);
        }

        [TestMethod]
        public void HistoryDoesNotCompletes()
        {
            var ts = new TestScheduler();

            var history = ts.CreateColdObservable(
                OnNext<ICacheNotification>(1, new DummyNotification { EventId = 1 }));

            var observer = ts.CreateObserver<ICacheNotification>();

            history.Combine(Observable.Never<ICacheNotification>()).Subscribe(observer);

            ts.AdvanceTo(2);

            ReactiveAssert.AreElementsEqual(
                new Recorded<Notification<ICacheNotification>> [] {}, 
                observer.Messages);
        }

        [TestMethod]
        public void HistoryCompletes()
        {
            var ts = new TestScheduler();

            var history = ts.CreateColdObservable(
                OnNext<ICacheNotification>(1, new DummyNotification { EventId = 1 }),
                OnCompleted<ICacheNotification>(2));
            
            var observer = ts.CreateObserver<ICacheNotification>();

            history.Combine(Observable.Never<ICacheNotification>()).Subscribe(observer);

            ts.AdvanceTo(2);

            ReactiveAssert.AreElementsEqual(
                new[]
                    {
                        OnNext<ICacheNotification>(2, n => n.EventId == 1)
                    },
                observer.Messages);
        }



        [TestMethod]
        public void HistoryMultipleValues()
        {
            var ts = new TestScheduler();

            var history = ts.CreateColdObservable(
                OnNext<ICacheNotification>(1, new DummyNotification { EventId = 1 }),
                OnNext<ICacheNotification>(4, new DummyNotification { EventId = 2 }),
                OnNext<ICacheNotification>(9, new DummyNotification { EventId = 3 }),
                OnCompleted<ICacheNotification>(10));

            var observer = ts.CreateObserver<ICacheNotification>();

            history.Combine(Observable.Never<ICacheNotification>()).Subscribe(observer);

            ts.AdvanceTo(20);

            ReactiveAssert.AreElementsEqual(
                new[]
                    {
                        OnNext<ICacheNotification>(10, n => n.EventId == 1),
                        OnNext<ICacheNotification>(10, n => n.EventId == 2),
                        OnNext<ICacheNotification>(10, n => n.EventId == 3)
                    },
                observer.Messages);
        }

        [TestMethod]
        public void NoLiveValuesIfHistoryHasntCompleted()
        {
            var ts = new TestScheduler();

            var live = ts.CreateHotObservable(
                OnNext<ICacheNotification>(1, new DummyNotification { EventId = 1 }));

            var observer = ts.CreateObserver<ICacheNotification>();

            Observable.Never<ICacheNotification>().
                Combine(live).Subscribe(observer);

            ts.AdvanceTo(2);

            ReactiveAssert.AreElementsEqual(
                new Recorded<Notification<ICacheNotification>>[] { },
                observer.Messages);
        }

        [TestMethod]
        public void PassLiveThroughWhenHistoryCompleted()
        {
            var ts = new TestScheduler();

            var live = ts.CreateHotObservable(
                            OnNext<ICacheNotification>(1, new DummyNotification { EventId = 1 }));

            var observer = ts.CreateObserver<ICacheNotification>();

            Observable.Empty<ICacheNotification>().Combine(live).Subscribe(observer);

            ts.AdvanceTo(20);

            ReactiveAssert.AreElementsEqual(
                new[]
                    {
                        OnNext<ICacheNotification>(1, n => n.EventId == 1)
                    },
                observer.Messages);
        }

        [TestMethod]
        public void BufferLiveUntilHistoryCompletes()
        {
            var ts = new TestScheduler();

            var history = ts.CreateColdObservable(
                OnNext<ICacheNotification>(3, new DummyNotification { EventId = 1 }),
                OnCompleted<ICacheNotification>(5));

            var live = ts.CreateHotObservable(
                            OnNext<ICacheNotification>(4, new DummyNotification { EventId = 2 }));

            var observer = ts.CreateObserver<ICacheNotification>();

            history.Combine(live).Subscribe(observer);

            ts.AdvanceTo(20);

            ReactiveAssert.AreElementsEqual(
                new[]
                    {
                        OnNext<ICacheNotification>(5, n => n.EventId == 1),
                        OnNext<ICacheNotification>(5, n => n.EventId == 2)
                    },
                observer.Messages);
        }

        [TestMethod]
        public void CombineTest()
        {
            var testScheduler = new TestScheduler();

            var history = testScheduler.CreateColdObservable(
                OnNext(1L, new DummyNotification { EventId = 1 }),
                OnNext(2L, new DummyNotification { EventId = 2 }),
                OnNext(3L, new DummyNotification { EventId = 3 }),
                OnNext(4L, new DummyNotification { EventId = 4 }),
                OnCompleted(new DummyNotification(), 5L));

            var live = testScheduler.CreateHotObservable(
                OnNext(1L, new DummyNotification { EventId = 3 }),
                OnNext(2L, new DummyNotification { EventId = 4 }),
                OnNext(3L, new DummyNotification { EventId = 5 }),
                OnNext(4L, new DummyNotification { EventId = 6 }),
                OnNext(5L, new DummyNotification { EventId = 7 }),
                OnNext(6L, new DummyNotification { EventId = 8 }),
                OnNext(7L, new DummyNotification { EventId = 9 })
                );

            var observer = testScheduler.CreateObserver<ICacheNotification>();
            history.Combine(live).Subscribe(observer);
            
            testScheduler.AdvanceTo(6L);

            ReactiveAssert.AreElementsEqual(
                new[]
                    {
                        OnNext<ICacheNotification>(5, n => n.EventId == 1),
                        OnNext<ICacheNotification>(5, n => n.EventId == 2),
                        OnNext<ICacheNotification>(5, n => n.EventId == 3),
                        OnNext<ICacheNotification>(5, n => n.EventId == 4),
                        OnNext<ICacheNotification>(5, n => n.EventId == 5),
                        OnNext<ICacheNotification>(5, n => n.EventId == 6),
                        OnNext<ICacheNotification>(5, n => n.EventId == 7),
                        OnNext<ICacheNotification>(6, n => n.EventId == 8)
                    },
                observer.Messages);
        }
    }
}