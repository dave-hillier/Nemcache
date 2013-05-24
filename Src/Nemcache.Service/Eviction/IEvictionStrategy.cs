using System;
namespace Nemcache.Service.Eviction
{
    internal interface IEvictionStrategy : IDisposable
    {
        void EvictEntry();
    }
}