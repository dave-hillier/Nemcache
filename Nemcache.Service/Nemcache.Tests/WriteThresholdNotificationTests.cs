using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nemcache.Tests
{
    static class ReactiveExt
    {
        public static IObservable<T> MySample<T>(this IObservable<T> observable, TimeSpan minInterval, IScheduler scheduler)
        {
            DateTimeOffset lastUpdate = DateTimeOffset.MinValue;
            return Observable.Create<T>(obs => observable.Subscribe(s =>
                {
                    var now = scheduler.Now;
                    if (now - lastUpdate > minInterval)
                    {
                        obs.OnNext(s);
                        lastUpdate = now;
                    }
                }, obs.OnError, obs.OnCompleted));
        }
    }
    [TestClass]
    public class WriteThresholdNotificationTests : ReactiveTest
    {
        class WriteThresholdNotification
        {
            private readonly long _writeThreshold;
            private readonly TimeSpan _minInterval;

            public WriteThresholdNotification(long writeThreshold, TimeSpan minInterval)
            {
                _writeThreshold = writeThreshold;
                _minInterval = minInterval;
            }

            public IObservable<Unit> Create(IObservable<long> logWriteNotifications, IScheduler scheduler)
            {
                var writesAccumulatedOverThresholdNotifications = logWriteNotifications.
                    Scan(0L, (writeAcc, newWrite) => writeAcc + newWrite).
                    Where(writeAcc => writeAcc > _writeThreshold).
                    Take(1).
                    Repeat();

                return writesAccumulatedOverThresholdNotifications.
                    MySample(_minInterval, scheduler).
                    Select(_ => new Unit());

            }
        }
        [TestMethod]
        public void NoWritesNoCompacts()
        {
            var testScheduler = new TestScheduler();
            var results = testScheduler.CreateObserver<Unit>();
            var factory = new WriteThresholdNotification(1, TimeSpan.FromTicks(100));

            var observable = factory.Create(Observable.Never<long>(), testScheduler);
            observable.Subscribe(results);
            testScheduler.AdvanceBy(1000);

            Assert.AreEqual(0, results.Messages.Count);
        }

        [TestMethod]
        public void SingleWriteBelowThresholdNoCompacts()
        {
            var testScheduler = new TestScheduler();
            var results = testScheduler.CreateObserver<Unit>();

            var logWriteNotifications = testScheduler.CreateHotObservable(OnNext(1u, 99L));
            var threshold = 100;
            var factory = new WriteThresholdNotification(threshold, TimeSpan.FromTicks(100));


            var observable = factory.Create(logWriteNotifications, testScheduler);
            observable.Subscribe(results);
            testScheduler.AdvanceBy(1000);

            Assert.AreEqual(0, results.Messages.Count);
        }


        [TestMethod]
        public void AccumulateAboveThresholdCompacts()
        {
            var testScheduler = new TestScheduler();
            var results = testScheduler.CreateObserver<Unit>();

            var logWriteNotifications = testScheduler.CreateHotObservable(
                OnNext(101, 101L),
                OnNext(102, 101L)
                );
            var threshold = 200;
            var factory = new WriteThresholdNotification(threshold, TimeSpan.FromTicks(3));


            var observable = factory.Create(logWriteNotifications, testScheduler);

            observable.Subscribe(results);
            testScheduler.AdvanceBy(103);

            ReactiveAssert.AreElementsEqual(new[] { OnNext<Unit>(102, _ => true) }, results.Messages);
        }

        [TestMethod]
        public void DontCompactTooFrequently()
        {
            var testScheduler = new TestScheduler();
            var results = testScheduler.CreateObserver<Unit>();

            var logWriteNotifications = testScheduler.CreateHotObservable(
                OnNext(101, 1L),
                OnNext(102, 1L),
                OnNext(103, 1L),
                OnNext(104, 1L)
                );
            var threshold = 1;
            var factory = new WriteThresholdNotification(threshold, TimeSpan.FromTicks(8));


            var observable = factory.Create(logWriteNotifications, testScheduler);
            observable.Subscribe(results);
            testScheduler.AdvanceTo(1000);

            ReactiveAssert.AreElementsEqual(new[] { OnNext<Unit>(102, _ => true) }, results.Messages);
        }

        [TestMethod]
        public void EventsSeparatedByInterval()
        {
            var testScheduler = new TestScheduler();
            var results = testScheduler.CreateObserver<Unit>();

            var logWriteNotifications = testScheduler.CreateHotObservable(
                OnNext(102, 1L),
                OnNext(103, 1L),
                OnNext(301, 1L),
                OnNext(302, 1L)
                );
            var threshold = 1;
            var factory = new WriteThresholdNotification(threshold, TimeSpan.FromTicks(99));

            var observable = factory.Create(logWriteNotifications, testScheduler);
            observable.Subscribe(results);
            testScheduler.AdvanceBy(500);

            ReactiveAssert.AreElementsEqual(new[]
                {
                    OnNext<Unit>(103, _ => true),
                    OnNext<Unit>(302, _ => true)
                }, results.Messages);
        }
    }
}