using NUnit.Framework;
using Nemcache.DynamoService.Routing;
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
}
