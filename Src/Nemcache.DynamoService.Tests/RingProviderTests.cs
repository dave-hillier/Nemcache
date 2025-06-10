using NUnit.Framework;
using Nemcache.DynamoService.Routing;
using System.Linq;

namespace Nemcache.DynamoService.Tests;

[TestFixture]
public class RingProviderTests
{
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
}
