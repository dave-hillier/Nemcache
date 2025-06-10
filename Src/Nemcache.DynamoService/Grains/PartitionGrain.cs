using System;
using System.Linq;
using System.Threading.Tasks;
using Nemcache.DynamoService.Routing;
using Nemcache.DynamoService.Services;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;
        private IMemCache? _cache;
        private readonly IMemCacheFactory _cacheFactory;
        private readonly IFileSystem _fileSystem;
        private ICachePersistence? _persistence;
        public PartitionGrain(IMemCacheFactory cacheFactory, RingProvider ring, IFileSystem fileSystem)
            _cacheFactory = cacheFactory;
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
            _cache!.Store(key, 0, value, DateTime.MaxValue);
            _cache!.Store(key, 0, value, DateTime.MaxValue);

            var entry = _cache!.Get(key);
            var replicaKeys = _ring.GetReplicas(key).ToArray();
            // Skip the local partition when querying replicas
            foreach (var replicaKey in replicaKeys.Where(k => k != this.GetPrimaryKeyString()))
                    // Store locally for read-repair
                    _cache.Store(key, 0, value, DateTime.MaxValue);

                    // Forward to the remaining replicas for full repair
                    foreach (var forwardKey in replicaKeys.Where(k => k != replicaKey && k != this.GetPrimaryKeyString()))
                    {
                        var forwardReplica = GrainFactory.GetGrain<IPartitionGrain>(forwardKey);
                        await forwardReplica.PutReplicaAsync(key, value);
                    }

            var entry = _cache!.Get(key);
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
            var entry = _cache!.Get(key);
            return Task.FromResult<byte[]?>(entry.Data);
        }
    }
}
