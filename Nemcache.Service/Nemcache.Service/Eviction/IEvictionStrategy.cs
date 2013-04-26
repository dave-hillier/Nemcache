namespace Nemcache.Service.Eviction
{
    internal interface IEvictionStrategy
    {
        void EvictEntry();
    }
}