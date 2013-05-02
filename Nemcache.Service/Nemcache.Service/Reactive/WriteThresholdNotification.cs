using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Nemcache.Service
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
                RateLimit(_minInterval, scheduler).
                Select(_ => new Unit());
        }
    }
}