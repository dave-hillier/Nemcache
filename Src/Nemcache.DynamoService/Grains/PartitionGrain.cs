using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
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

        public PartitionGrain(IMemCacheFactory cacheFactory, RingProvider ring, IFileSystem fileSystem)
        {
            _cacheFactory = cacheFactory;
            _ring = ring;
            _fileSystem = fileSystem;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _cache = _cacheFactory.Create();
            var logPath = $"{this.GetPrimaryKeyString()}.log";
            var archiver = new StreamArchiver(_fileSystem, logPath, (MemCache)_cache, 10_000);
            var restorer = new CacheRestorer(_cache, _fileSystem, logPath);
            _persistence = new StreamPersistence(archiver, restorer);
            _cache.Notifications.Subscribe(_persistence);
            _persistence.Restore();
            return base.OnActivateAsync(cancellationToken);
        }

        public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            _persistence?.Dispose();
            _cache?.Dispose();
            return base.OnDeactivateAsync(reason, cancellationToken);
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
            if (_cache!.TryGet(key, out var entry) && entry.Data != null)
            {
                return entry.Data;
            }

            var replicaKeys = _ring.GetReplicas(key).ToArray();
            // Skip the local partition when querying replicas
            foreach (var replicaKey in replicaKeys.Where(k => k != this.GetPrimaryKeyString()))
            {
                var replica = GrainFactory.GetGrain<IPartitionGrain>(replicaKey);
                var value = await replica.GetReplicaAsync(key);
                if (value != null)
                {
                    // Store locally for read-repair
                    _cache.Store(key, 0, value, DateTime.MaxValue);

                    // Forward to the remaining replicas for full repair
                    foreach (var forwardKey in replicaKeys.Where(k => k != replicaKey && k != this.GetPrimaryKeyString()))
                    {
                        var forwardReplica = GrainFactory.GetGrain<IPartitionGrain>(forwardKey);
                        await forwardReplica.PutReplicaAsync(key, value);
                    }

                    return value;
                }
            }

            return null;
        }

        public Task<byte[]?> GetReplicaAsync(string key)
        {
            if (_cache.TryGet(key, out var entry))
            {
                return Task.FromResult<byte[]?>(entry.Data);
            }

            return Task.FromResult<byte[]?>(null);
        }
    }
}
