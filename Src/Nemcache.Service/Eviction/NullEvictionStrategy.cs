﻿namespace Nemcache.Service.Eviction
{
    internal class NullEvictionStrategy : IEvictionStrategy
    {
        public void EvictEntry()
        {
        }

        public void Dispose()
        {
            
        }
    }
}