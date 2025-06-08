namespace Nemcache.Storage.Eviction
{
    public class NullEvictionStrategy : IEvictionStrategy
    {
        public void EvictEntry()
        {
        }

        public void Dispose()
        {
            
        }
    }
}