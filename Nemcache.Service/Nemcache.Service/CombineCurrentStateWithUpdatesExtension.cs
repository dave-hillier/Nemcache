using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Nemcache.Service.Notifications;

namespace Nemcache.Service
{
    internal static class CombineCurrentStateWithUpdatesExtension
    {
        public static IObservable<ICacheNotification> Combine(
            this IObservable<ICacheNotification> historic,
            IObservable<ICacheNotification> live)
        {
            return CombinePrivate(historic, live).
                DistinctUntilChanged(n => n.EventId);
        }

        private static IObservable<ICacheNotification> CombinePrivate(
            IObservable<ICacheNotification> historic, 
            IObservable<ICacheNotification> live)
        {
            return Observable.Create<ICacheNotification>(obs =>
                {
                    var disposable = new CompositeDisposable();
                    var notificationBuffer = new ConcurrentBag<ICacheNotification>();

                    var liveSubscription = live.Subscribe(cacheNotification =>
                        {
                            if (notificationBuffer != null)
                            {
                                notificationBuffer.Add(cacheNotification);
                            }
                            else
                            {
                                obs.OnNext(cacheNotification);
                            }
                        });
                    disposable.Add(liveSubscription);

                    var historicSubscription = historic.Subscribe(
                        n => { notificationBuffer.Add(n); },
                        () =>
                            {
                                var cacheNotifications = notificationBuffer.
                                    OrderBy(n => n.EventId);

                                foreach (var cacheNotification in cacheNotifications)
                                {
                                    obs.OnNext(cacheNotification);
                                }
                                notificationBuffer = null;
                            });
                    disposable.Add(historicSubscription);
                    return disposable;
                });
        }
    }
}