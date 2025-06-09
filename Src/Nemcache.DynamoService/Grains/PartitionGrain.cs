using System;
using System.Linq;
using System.Threading.Tasks;
using Nemcache.DynamoService.Routing;
using Nemcache.Storage;
using Orleans;

namespace Nemcache.DynamoService.Grains
{
    public class PartitionGrain : Grain, IPartitionGrain
    {
        private readonly IMemCache _cache;
        private readonly RingProvider _ring;
        private const int ReplicaCount = 3;

        public PartitionGrain(IMemCache cache, RingProvider ring)
        {
            _cache = cache;
            _ring = ring;
        }

        public async Task PutAsync(string key, byte[] value)
        {
            _cache.Store(key, 0, value, DateTime.MaxValue);

            var replicas = _ring.GetReplicas(key).Skip(1);
            foreach (var replicaKey in replicas)
            {
                var replica = GrainFactory.GetGrain<IPartitionGrain>(replicaKey);
                await replica.PutReplicaAsync(key, value);
            }
        }

        public Task PutReplicaAsync(string key, byte[] value)
        {
            _cache.Store(key, 0, value, DateTime.MaxValue);
            return Task.CompletedTask;
        }

        public async Task<byte[]?> GetAsync(string key)
        {
            var entry = _cache.Get(key);
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
            var entry = _cache.Get(key);
            return Task.FromResult<byte[]?>(entry.Data);
        }
    }
}
