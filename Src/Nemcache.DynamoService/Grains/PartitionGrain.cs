using System;
using System.Linq;
using System.Threading.Tasks;
using Nemcache.DynamoService.Routing;
using Nemcache.DynamoService.Services;
using Nemcache.Storage;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;
using Orleans;

namespace Nemcache.DynamoService.Grains
{
    public class PartitionGrain : Grain, IPartitionGrain
    {
        private IMemCache? _cache;
        private readonly RingProvider _ring;
        private readonly IMemCacheFactory _cacheFactory;
        private readonly IFileSystem _fileSystem;
        private ICachePersistence? _persistence;
        private const int ReplicaCount = 3;

        public PartitionGrain(IMemCacheFactory cacheFactory, RingProvider ring, IFileSystem fileSystem)
        {
            _cacheFactory = cacheFactory;
            _ring = ring;
            _fileSystem = fileSystem;
        }

        public override Task OnActivateAsync()
        {
            _cache = _cacheFactory.Create();
            var logPath = $"{this.GetPrimaryKeyString()}.log";
            var archiver = new StreamArchiver(_fileSystem, logPath, (MemCache)_cache, 10_000);
            var restorer = new CacheRestorer(_cache, _fileSystem, logPath);
            _persistence = new StreamPersistence(archiver, restorer);
            _cache.Notifications.Subscribe(_persistence);
            _persistence.Restore();
            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _persistence?.Dispose();
            _cache?.Dispose();
            return base.OnDeactivateAsync();
        }

        public async Task PutAsync(string key, byte[] value)
        {
            _cache!.Store(key, 0, value, DateTime.MaxValue);

            var replicas = _ring.GetReplicas(key).Skip(1);
            foreach (var replicaKey in replicas)
            {
                var replica = GrainFactory.GetGrain<IPartitionGrain>(replicaKey);
                await replica.PutReplicaAsync(key, value);
            }
        }

        public Task PutReplicaAsync(string key, byte[] value)
        {
            _cache!.Store(key, 0, value, DateTime.MaxValue);
            return Task.CompletedTask;
        }

        public async Task<byte[]?> GetAsync(string key)
        {
            var entry = _cache!.Get(key);
            if (entry.Data != null)
            {
                return entry.Data;
            }

            var replicas = _ring.GetReplicas(key).Skip(1);
            foreach (var replicaKey in replicas)
            {
                var replica = GrainFactory.GetGrain<IPartitionGrain>(replicaKey);
                var value = await replica.GetReplicaAsync(key);
                if (value != null)
                {
                    return value;
                }
            }

            return null;
        }

        public Task<byte[]?> GetReplicaAsync(string key)
        {
            var entry = _cache!.Get(key);
            return Task.FromResult<byte[]?>(entry.Data);
        }
    }
}
