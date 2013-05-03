using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Nemcache.Service.Reactive
{
    internal static class ThresholdExtension
    {
        public static IObservable<Unit> ThresholdReached(this IObservable<long> observable, long threshold)
        {
            return observable.
                Scan(0L, (acc, increment) => acc + increment).
                Where(acc => acc > threshold).
                Select(_ => new Unit()).
                Take(1);
        }
    }
}