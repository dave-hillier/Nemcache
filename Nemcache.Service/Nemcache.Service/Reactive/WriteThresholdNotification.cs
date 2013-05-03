﻿using System;
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

        public IObservable<Unit> Create(IObservable<long> notifications)
        {
            return notifications.ThresholdReached(_writeThreshold).
                                 Repeat().
                                 RateLimit(_minInterval, _scheduler);
        }


    }
}