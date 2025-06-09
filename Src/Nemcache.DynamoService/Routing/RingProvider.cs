using System.Collections.Generic;
using System.Linq;

namespace Nemcache.DynamoService.Routing
{
    /// <summary>
    /// Provides partition selection using a consistent hash ring.
    /// </summary>
    public class RingProvider
    {
        private readonly ConsistentHashRing _ring;
        private readonly int _replicaCount;

        public RingProvider(int partitionCount, int replicaCount)
        {
            _replicaCount = replicaCount;
            var nodes = Enumerable.Range(0, partitionCount)
                .Select(i => $"partition-{i}");
            _ring = new ConsistentHashRing(nodes);
        }

        public IEnumerable<string> GetReplicas(string key)
        {
            return _ring.GetNodes(key, _replicaCount);
        }
    }
}
