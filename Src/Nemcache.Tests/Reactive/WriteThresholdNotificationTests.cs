using System;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Nemcache.Storage.Reactive;

namespace Nemcache.Tests.Reactive
{
    [TestFixture]
    public class WriteThresholdNotificationTests : ReactiveTest
    {
        [Test]
        public void NoWritesNoCompacts()
        {
            var testScheduler = new TestScheduler();
            var results = testScheduler.CreateObserver<Unit>();
            var factory = new WriteThresholdNotification(1, TimeSpan.FromTicks(100), testScheduler);

            var observable = factory.Create(Observable.Never<long>());
            observable.Subscribe(results);
            testScheduler.AdvanceBy(1000);

            Assert.AreEqual(0, results.Messages.Count);
        }

        [Test]
        public void SingleWriteBelowThresholdNoCompacts()
        {
            var testScheduler = new TestScheduler();
            var results = testScheduler.CreateObserver<Unit>();

            var logWriteNotifications = testScheduler.CreateHotObservable(OnNext(1u, 99L));
            const long threshold = 100;
            var factory = new WriteThresholdNotification(threshold, TimeSpan.FromTicks(100), testScheduler);


            var observable = factory.Create(logWriteNotifications);
            observable.Subscribe(results);
            testScheduler.AdvanceBy(1000);

            Assert.AreEqual(0, results.Messages.Count);
        }


        [Test]
        public void AccumulateAboveThresholdCompacts()
        {
            var testScheduler = new TestScheduler();
            var results = testScheduler.CreateObserver<Unit>();

            var logWriteNotifications = testScheduler.CreateHotObservable(
                OnNext(101, 101L),
                OnNext(102, 101L)
                );
            const int threshold = 200;
            var factory = new WriteThresholdNotification(threshold, TimeSpan.FromTicks(3), testScheduler);

            var observable = factory.Create(logWriteNotifications);

            observable.Subscribe(results);
            testScheduler.AdvanceBy(103);

            ReactiveAssert.AreElementsEqual(new[] {OnNext<Unit>(102, _ => true)}, results.Messages);
        }

        [Test]
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
            const int threshold = 1;
            var factory = new WriteThresholdNotification(threshold, TimeSpan.FromTicks(8), testScheduler);


            var observable = factory.Create(logWriteNotifications);
            observable.Subscribe(results);
            testScheduler.AdvanceTo(1000);

            ReactiveAssert.AreElementsEqual(new[] {OnNext<Unit>(102, _ => true)}, results.Messages);
        }

        [Test]
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
            const int threshold = 1;
            var factory = new WriteThresholdNotification(threshold, TimeSpan.FromTicks(99), testScheduler);

            var observable = factory.Create(logWriteNotifications);
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