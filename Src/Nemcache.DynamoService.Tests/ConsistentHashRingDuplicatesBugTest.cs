using System.Linq;
using NUnit.Framework;
using Nemcache.DynamoService.Routing;

namespace Nemcache.Tests.BugHuntTests
{
    /// <summary>
    /// Bug: ConsistentHashRing.GetNodes returns duplicate physical nodes because
    /// it walks virtual-node positions without deduplicating the physical nodes.
    /// Two adjacent virtual nodes can map to the same physical node, so GetNodes(key, 3)
    /// might return ["node-A", "node-A", "node-B"] instead of 3 distinct nodes.
    /// This causes reduced replication factor and potential deadlocks in Orleans grains.
    /// See ConsistentHashRing.cs lines 65-78.
    /// </summary>
    [TestFixture]
    public class ConsistentHashRingDuplicatesBugTest
    {
        [Test]
        public void GetNodesShouldReturnDistinctPhysicalNodes()
        {
            // With 100 virtual nodes per physical node and 4 physical nodes,
            // adjacent virtual-node slots can (and do) map to the same physical node.
            // GetNodes should deduplicate and return N distinct physical nodes.
            var nodes = new[] { "partition-0", "partition-1", "partition-2", "partition-3" };
            var ring = new ConsistentHashRing(nodes, virtualNodes: 100);

            // Try many keys - at least one should expose the dedup bug
            int duplicateCount = 0;
            for (int i = 0; i < 1000; i++)
            {
                var result = ring.GetNodes($"test-key-{i}", 3).ToArray();
                if (result.Distinct().Count() < 3)
                {
                    duplicateCount++;
                }
            }

            // If GetNodes properly deduplicates, no key should ever return duplicates
            // when requesting 3 replicas from 4 physical nodes
            Assert.AreEqual(0, duplicateCount,
                $"GetNodes returned duplicate physical nodes for {duplicateCount}/1000 keys. " +
                "GetNodes should deduplicate virtual-to-physical node mappings.");
        }

        [Test]
        public void GetNodesWithTwoNodesShouldNeverReturnSameNodeTwice()
        {
            var ring = new ConsistentHashRing(new[] { "A", "B" }, virtualNodes: 50);

            for (int i = 0; i < 100; i++)
            {
                var result = ring.GetNodes($"key-{i}", 2).ToArray();
                Assert.AreEqual(2, result.Distinct().Count(),
                    $"GetNodes(key-{i}, 2) returned [{string.Join(", ", result)}] - " +
                    "should be 2 distinct nodes");
            }
        }
    }
}
