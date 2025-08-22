using NUnit.Framework;
using Nemcache.DynamoService.Routing;
using System;
using System.Linq;

namespace Nemcache.DynamoService.Tests;

[TestFixture]
public class ConsistentHashRingTests
{
    [Test]
    public void Distribution_is_fair_across_nodes()
    {
        var nodes = Enumerable.Range(0, 4).Select(i => $"n{i}").ToArray();
        var ring = new ConsistentHashRing(nodes);
        var counts = nodes.ToDictionary(n => n, _ => 0);

        for (int i = 0; i < 1000; i++)
        {
            var node = ring.GetNode($"key-{i}");
            counts[node]++;
        }

        var avg = counts.Values.Average();
        foreach (var c in counts.Values)
        {
            Assert.That(c, Is.InRange(avg * 0.5, avg * 1.5));
        }
    }

    [Test]
    public void Key_maps_consistently_through_add_and_remove()
    {
        var ring = new ConsistentHashRing(new[] { "a", "b" }, virtualNodes: 1);
        const string key = "key0";

        // Initial mapping
        Assert.That(ring.GetNode(key), Is.EqualTo("b"));

        // After adding a node
        ring.AddNode("c");
        Assert.That(ring.GetNode(key), Is.EqualTo("c"));
        Assert.That(ring.GetNode(key), Is.EqualTo("c"));

        // After removing the node
        ring.RemoveNode("c");
        Assert.That(ring.GetNode(key), Is.EqualTo("b"));
    }

    [Test]
    public void GetNodes_returns_unique_nodes_and_wraps_when_exceeding_ring()
    {
        var ring = new ConsistentHashRing(new[] { "a", "b", "c" }, virtualNodes: 1);
        const string key = "key0";

        var firstThree = ring.GetNodes(key, 3).ToArray();
        Assert.That(firstThree, Is.EqualTo(new[] { "c", "b", "a" }));
        Assert.That(firstThree.Distinct().Count(), Is.EqualTo(3));

        var fiveNodes = ring.GetNodes(key, 5).ToArray();
        Assert.That(fiveNodes, Is.EqualTo(new[] { "c", "b", "a", "c", "b" }));
    }

    [Test]
    public void GetNode_on_empty_ring_throws()
    {
        var ring = new ConsistentHashRing(Array.Empty<string>());
        var ex = Assert.Throws<InvalidOperationException>(() => ring.GetNode("any"));
        Assert.That(ex!.Message, Is.EqualTo("Hash ring is empty"));
    }
}
