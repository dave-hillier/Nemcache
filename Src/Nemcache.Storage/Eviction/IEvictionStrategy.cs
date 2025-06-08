using System;
namespace Nemcache.Storage.Eviction
{
    internal interface IEvictionStrategy : IDisposable
    {
        void EvictEntry();
    }
}