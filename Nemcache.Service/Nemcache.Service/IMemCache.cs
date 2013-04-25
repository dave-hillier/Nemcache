using System;
using System.Collections.Generic;
namespace Nemcache.Service
{
    interface ICacheNotification 
    {
        // TODO: A sequence Id?
    }

    interface IKeyCacheNotification 
    { 
        string Key { get; }
    }

    class Clear : ICacheNotification
    {
    }

    class Store : IKeyCacheNotification
    {
        public string Key {get; set; }

        public byte[] Data { get; set; }
    }

    class Touch : IKeyCacheNotification
    {
        public string Key {get; set; }
    }

    class Remove : IKeyCacheNotification
    {
        public string Key {get; set; }
    }

    interface IMemCache
    {
        bool Add(string key, ulong flags, DateTime exptime, byte[] data);
        bool Append(string key, ulong flags, DateTime exptime, byte[] data, bool prepend);
        int Capacity { get; set; }
        bool Cas(string key, ulong flags, DateTime exptime, ulong casUnique, byte[] newData);
        void Clear();
        IEnumerable<string> Keys { get; }
        bool Mutate(string commandName, string key, ulong incr, out byte[] resultDataOut);
        bool Remove(string key);
        bool Replace(string key, ulong flags, DateTime exptime, byte[] data);
        IEnumerable<KeyValuePair<string, MemCache.CacheEntry>> Retrieve(IEnumerable<string> keys);
        bool Store(string key, ulong flags, DateTime exptime, byte[] data);
        bool Touch(string key, DateTime exptime);

        int Used { get; }

        IObservable<ICacheNotification> Notifications { get; }
    }
}
