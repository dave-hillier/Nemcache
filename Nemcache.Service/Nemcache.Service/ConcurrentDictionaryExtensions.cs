using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    // Hides the generics
    class KeyValuePair
    {
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }


    static class ConcurrentDictionaryExtensions
    {
        public static bool TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict,
            TKey key, Func<TValue, TValue> updateFactory)
        {
            TValue curValue;
            while (dict.TryGetValue(key, out curValue))
            {
                if (dict.TryUpdate(key, updateFactory(curValue), curValue))
                    return true;
                //if we're looping either the key was removed by another thread, or another thread
                //changed the value, so we start again.
            }
            return false;
        }
    }
}
