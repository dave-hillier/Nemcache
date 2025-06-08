using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Nemcache.Storage.Notifications;
using Nemcache.Storage.Reactive;

namespace Nemcache.Tests
{
    [TestFixture]
    public class CombineCurrentStateWithUpdatesTests : ReactiveTest
    {
        [Test]
        public void DummyToString()
        {
            var s = new DummyNotification {EventId = 123}.ToString();
            Assert.AreEqual("Notification: 123", s);
        }

        [Test]
        public void HistoryDoesNotCompletes()
        {
            var ts = new TestScheduler();

            var history = ts.CreateColdObservable(
                OnNext<ICacheNotification>(1, new DummyNotification {EventId = 1}));

            var observer = ts.CreateObserver<ICacheNotification>();

            history.Combine(Observable.Never<ICacheNotification>()).Subscribe(observer);

            ts.AdvanceTo(2);

            ReactiveAssert.AreElementsEqual(
                new Recorded<Notification<ICacheNotification>>[] {},
                observer.Messages);
        }

        [Test]
        public void HistoryCompletes()
        {
            var ts = new TestScheduler();

            var history = ts.CreateColdObservable(
                OnNext<ICacheNotification>(1, new DummyNotification {EventId = 1}),
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


        [Test]
        public void HistoryMultipleValues()
        {
            var ts = new TestScheduler();

            var history = ts.CreateColdObservable(
                OnNext<ICacheNotification>(1, new DummyNotification {EventId = 1}),
                OnNext<ICacheNotification>(4, new DummyNotification {EventId = 2}),
                OnNext<ICacheNotification>(9, new DummyNotification {EventId = 3}),
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

        [Test]
        public void NoLiveValuesIfHistoryHasntCompleted()
        {
            var ts = new TestScheduler();

            var live = ts.CreateHotObservable(
                OnNext<ICacheNotification>(1, new DummyNotification {EventId = 1}));

            var observer = ts.CreateObserver<ICacheNotification>();

            Observable.Never<ICacheNotification>().
                       Combine(live).Subscribe(observer);

            ts.AdvanceTo(2);

            ReactiveAssert.AreElementsEqual(
                new Recorded<Notification<ICacheNotification>>[] {},
                observer.Messages);
        }

        [Test]
        public void PassLiveThroughWhenHistoryCompleted()
        {
            var ts = new TestScheduler();

            var live = ts.CreateHotObservable(
                OnNext<ICacheNotification>(1, new DummyNotification {EventId = 1}));

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

        [Test]
        public void BufferLiveUntilHistoryCompletes()
        {
            var ts = new TestScheduler();

            var history = ts.CreateColdObservable(
                OnNext<ICacheNotification>(3, new DummyNotification {EventId = 1}),
                OnCompleted<ICacheNotification>(5));

            var live = ts.CreateHotObservable(
                OnNext<ICacheNotification>(4, new DummyNotification {EventId = 2}));

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

        [Test]
        public void CombineTest()
        {
            var testScheduler = new TestScheduler();

            var history = testScheduler.CreateColdObservable(
                OnNext<ICacheNotification>(1L, new DummyNotification {EventId = 1}),
                OnNext<ICacheNotification>(2L, new DummyNotification {EventId = 2}),
                OnNext<ICacheNotification>(3L, new DummyNotification {EventId = 3}),
                OnNext<ICacheNotification>(4L, new DummyNotification {EventId = 4}),
                OnCompleted<ICacheNotification>(5L));

            var live = testScheduler.CreateHotObservable(
                OnNext<ICacheNotification>(1L, new DummyNotification {EventId = 3}),
                OnNext<ICacheNotification>(2L, new DummyNotification {EventId = 4}),
                OnNext<ICacheNotification>(3L, new DummyNotification {EventId = 5}),
                OnNext<ICacheNotification>(4L, new DummyNotification {EventId = 6}),
                OnNext<ICacheNotification>(5L, new DummyNotification {EventId = 7}),
                OnNext<ICacheNotification>(6L, new DummyNotification {EventId = 8}),
                OnNext<ICacheNotification>(7L, new DummyNotification {EventId = 9})
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

        private class DummyNotification : ICacheNotification
        {
            public int EventId { get; set; }

            public override string ToString()
            {
                return "Notification: " + EventId.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}