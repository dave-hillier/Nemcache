using System.Reactive.Concurrency;
using Nemcache.Storage;

namespace Nemcache.DynamoService.Services
{
    public class MemCacheFactory : IMemCacheFactory
    {
        private readonly ulong _capacity;
        private readonly IScheduler _scheduler;

        public MemCacheFactory(ulong capacity, IScheduler scheduler)
        {
            _capacity = capacity;
            _scheduler = scheduler;
        }

        public IMemCache Create()
        {
            return new MemCache(_capacity, _scheduler);
        }
    }
}
