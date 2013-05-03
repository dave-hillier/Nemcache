using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Nemcache.Service.Reactive
{
    class WriteThresholdNotification
    {
        private readonly long _writeThreshold;
        private readonly TimeSpan _minInterval;
        private readonly IScheduler _scheduler;

        public WriteThresholdNotification(long writeThreshold, TimeSpan minInterval, IScheduler scheduler)
        {
            _writeThreshold = writeThreshold;
            _minInterval = minInterval;
            _scheduler = scheduler;
        }

        public IObservable<Unit> Create(IObservable<long> logWriteNotifications)
        {
            var writesAccumulatedOverThresholdNotifications = logWriteNotifications.
                Scan(0L, (writeAcc, newWrite) => writeAcc + newWrite).
                Where(writeAcc => writeAcc > _writeThreshold).
                Take(1).
                Repeat();

            return writesAccumulatedOverThresholdNotifications.
                RateLimit(_minInterval, _scheduler).
                Select(_ => new Unit());
        }
    }
}