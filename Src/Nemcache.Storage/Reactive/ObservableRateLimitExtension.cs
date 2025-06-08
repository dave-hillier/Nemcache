using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Nemcache.Storage
{
    internal static class ObservableRateLimitExtension
    {
        public static IObservable<T> RateLimit<T>(this IObservable<T> observable, TimeSpan minInterval,
                                                  IScheduler scheduler)
        {
            var lastUpdate = DateTimeOffset.MinValue;
            return Observable.Create<T>(obs => observable.Subscribe(s =>
                {
                    var now = scheduler.Now;
                    if (now - lastUpdate > minInterval)
                    {
                        lastUpdate = now;
                        obs.OnNext(s);
                    }
                }, obs.OnError, obs.OnCompleted));
        }
    }
}