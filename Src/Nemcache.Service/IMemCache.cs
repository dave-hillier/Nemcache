using System;
using System.Collections.Generic;
using Nemcache.Service.Notifications;

namespace Nemcache.Service
{
    public interface IMemCache : IDisposable
    {
        ulong Capacity { get; set; }

        ulong Used { get; }

        IEnumerable<string> Keys { get; }

        IEnumerable<KeyValuePair<string, CacheEntry>> Retrieve(IEnumerable<string> keys);

        bool Store(string key, ulong flags, byte[] data, DateTime exptime);

        bool Add(string key, ulong flags, DateTime exptime, byte[] data);

        bool Append(string key, ulong flags, DateTime exptime, byte[] data, bool prepend);

        bool Cas(string key, ulong flags, DateTime exptime, ulong casUnique, byte[] newData);

        bool Replace(string key, ulong flags, DateTime exptime, byte[] data);

        bool Touch(string key, DateTime exptime);

        bool Remove(string key);

        void Clear();

        bool Mutate(string key, ulong incr, out byte[] resultDataOut, bool positive);

        //IObservable<ICacheNotification> FullStateNotifications { get; }
        IObservable<ICacheNotification> Notifications { get; }

        CacheEntry Get(string s);

        bool TryGet(string s, out CacheEntry cacheEntry);
    }

    // TODO: consider implementing dictionary interface for above...
}