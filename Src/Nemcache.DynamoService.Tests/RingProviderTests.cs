using NUnit.Framework;
using Nemcache.DynamoService.Routing;
using System.Collections.Generic;
using System.Linq;

namespace Nemcache.DynamoService.Tests;

[TestFixture]
public class RingProviderTests
{
    private class StubRing : IRing
    {
        private readonly Dictionary<string, List<string>> _config = new();

        public void Configure(string key, params string[] partitions)
        {
            _config[key] = partitions.ToList();
        }

        public IEnumerable<string> GetNodes(string key, int count)
        {
            var nodes = _config[key];
            for (int i = 0; i < count; i++)
            {
                yield return nodes[i % nodes.Count];
            }
        }
    }

    [Test]
    public void GetReplicas_returns_even_distribution()
    {
        var provider = new RingProvider(partitionCount: 8, replicaCount: 3);
        var counts = Enumerable.Range(0, 8).Select(i => $"partition-{i}").ToDictionary(n => n, _ => 0);

        for (int i = 0; i < 1000; i++)
        {
            var partition = provider.GetReplicas($"key-{i}").First();
            counts[partition]++;
        }

        var avg = counts.Values.Average();
        foreach (var c in counts.Values)
        {
            Assert.That(c, Is.InRange(avg * 0.5, avg * 1.5));
        }
    }

    [Test]
    public void GetReplicas_returns_unique_partitions()
    {
        var ring = new StubRing();
        ring.Configure("key", "partition-0", "partition-1", "partition-2", "partition-3");
        var provider = new RingProvider(ring, replicaCount: 3);

        var replicas = provider.GetReplicas("key").ToList();

        Assert.That(replicas.Count, Is.EqualTo(3));
        Assert.That(replicas.Distinct().Count(), Is.EqualTo(3));
    }

    [Test]
    public void GetReplicas_handles_replicaCount_exceeding_partitions()
    {
        var ring = new StubRing();
        ring.Configure("key", "partition-0", "partition-1");
        var provider = new RingProvider(ring, replicaCount: 4);

        var replicas = provider.GetReplicas("key").ToList();

        Assert.That(replicas.Count, Is.EqualTo(4));
        Assert.That(replicas.Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public void Replica_sets_remain_stable_across_ring_changes()
    {
        var ring = new StubRing();
        ring.Configure("key", "partition-0", "partition-1", "partition-2");
        var provider = new RingProvider(ring, replicaCount: 3);

        var initial = provider.GetReplicas("key").ToList();

        // change ring configuration by adding a new partition at the end
        ring.Configure("key", "partition-0", "partition-1", "partition-2", "partition-3");
        var afterChange = provider.GetReplicas("key").ToList();

        Assert.That(afterChange, Is.EqualTo(initial));
    }
}
