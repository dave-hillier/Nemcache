using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Nemcache.Service.Notifications;

namespace Nemcache.Service
{
    // TODO: This probably doesnt need to exist... delete it
    internal class CacheCopy : IMemCache
    {
        private readonly IMemCache _innerCache;
        // TODO: needs a cache client

        public CacheCopy(IMemCache innerCache, IObservable<ICacheNotification> observableCache)
        {
            _innerCache = innerCache;

            observableCache.OfType<Store>().Subscribe(add => _innerCache.Add(add.Key, 0, DateTime.MaxValue, add.Data));
        }

        public IEnumerable<string> Keys
        {
            get { throw new NotImplementedException(); }
        }

        public bool Add(string key, ulong flags, DateTime exptime, byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool Append(string key, ulong flags, DateTime exptime, byte[] data, bool prepend)
        {
            throw new NotImplementedException();
        }

        public int Capacity
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool Cas(string key, ulong flags, DateTime exptime, ulong casUnique, byte[] newData)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Mutate(string commandName, string key, ulong incr, out byte[] resultDataOut)
        {
            throw new NotImplementedException();
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool Replace(string key, ulong flags, DateTime exptime, byte[] data)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<string, MemCache.CacheEntry>> Retrieve(IEnumerable<string> keys)
        {
            return _innerCache.Retrieve(keys);
        }

        public bool Store(string key, ulong flags, DateTime exptime, byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool Touch(string key, DateTime exptime)
        {
            throw new NotImplementedException();
        }

        public int Used
        {
            get { throw new NotImplementedException(); }
        }

        public IObservable<ICacheNotification> Notifications
        {
            get { throw new NotImplementedException(); }
        }
    }
}