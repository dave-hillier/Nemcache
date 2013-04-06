using System.Runtime.Caching;

namespace Nemcache.Service
{
    class ArrayMemoryCache : IArrayCache
    {
        private readonly MemoryCache _cache = new MemoryCache("ArrayCache");

        public MemoryCache Storage { get { return _cache; } }

        public bool Set(string key, byte[] value)
        {
            Storage.Set(key, value, new CacheItemPolicy());
            return true;
        }

        public byte[] Get(string key)
        {
            if(!Storage.Contains(key))
                return new byte[] {};
            return (byte[])Storage.Get(key);
        }
    }
}