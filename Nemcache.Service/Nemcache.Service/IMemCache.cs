using System;
using System.Collections.Generic;
using Nemcache.Service.Notifications;

namespace Nemcache.Service
{
   

    internal interface IMemCache
    {
        int Capacity { get; set; }
        int Used { get; }

        IObservable<ICacheNotification> Notifications { get; }
        bool Add(string key, ulong flags, DateTime exptime, byte[] data);
        bool Append(string key, ulong flags, DateTime exptime, byte[] data, bool prepend);
        bool Cas(string key, ulong flags, DateTime exptime, ulong casUnique, byte[] newData);
        void Clear();
        bool Mutate(string commandName, string key, ulong incr, out byte[] resultDataOut);
        bool Remove(string key);
        bool Replace(string key, ulong flags, DateTime exptime, byte[] data);
        IEnumerable<KeyValuePair<string, MemCache.CacheEntry>> Retrieve(IEnumerable<string> keys);
        bool Store(string key, ulong flags, DateTime exptime, byte[] data);
        bool Touch(string key, DateTime exptime);
    }
}