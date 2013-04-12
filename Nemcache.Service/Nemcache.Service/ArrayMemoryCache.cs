using System;
using System.Globalization;
using System.Runtime.Caching;
using System.Text;

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

        public void Remove(string key)
        {
            Storage.Remove(key);
        }

        public byte[] Increase(string key, ulong increment)
        {
            return ChangeValue(key, i => i + increment);
        }

        private byte[] ChangeValue(string key, Func<ulong, ulong> func)
        {

            if (Storage.Contains(key))
            {
                ulong value;
                if (ulong.TryParse(Encoding.ASCII.GetString(Get(key)), out value))
                {
                    var newValue = func(value);
                    var newULongString = newValue.ToString(CultureInfo.InvariantCulture);
                    Set(key, Encoding.ASCII.GetBytes(newULongString));
                    return Encoding.ASCII.GetBytes(newULongString + "\r\n");
                }
                return Encoding.ASCII.GetBytes(string.Format("ERROR {0} does not represent a 64-bit unsigned int\r\n", key)); // TODO: error?
            }
            return Encoding.ASCII.GetBytes("NOT_FOUND\r\n"); // TODO: error?
        }

        public byte[] Decrease(string key, ulong decrement)
        {
            return ChangeValue(key, i => i - decrement);
        }
    }
}