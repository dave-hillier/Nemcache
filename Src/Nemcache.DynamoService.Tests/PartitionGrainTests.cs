using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans.Hosting;
using Orleans.TestingHost;
using Nemcache.DynamoService.Grains;
using Nemcache.DynamoService.Routing;
using Nemcache.Storage;
using System.Reactive.Concurrency;

namespace Nemcache.DynamoService.Tests;

[TestFixture]
public class PartitionGrainTests
{
    private TestCluster? _cluster;
    private RingProvider? _provider;

    [OneTimeSetUp]
    public void SetUp()
    {
        var builder = new TestClusterBuilder(1);
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        _cluster = builder.Build();
        _cluster.Deploy();
        _provider = new RingProvider(partitionCount: 4, replicaCount: 3);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _cluster?.StopAllSilos();
        _cluster?.Dispose();
    }

    private class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IMemCache>(sp => new MemCache(1024 * 1024, Scheduler.Default));
                services.AddSingleton(new RingProvider(partitionCount: 4, replicaCount: 3));
            });
        }
    }

    [Test]
    public async Task PutAsync_replicates_to_all_nodes()
    {
        var key = "key0";
        var data = new byte[] {1,2,3};
        var replicas = _provider!.GetReplicas(key).ToArray();
        var primary = replicas.First();

        var grain = _cluster!.GrainFactory.GetGrain<IPartitionGrain>(primary);
        await grain.PutAsync(key, data);

        foreach (var replica in replicas)
        {
            var repGrain = _cluster.GrainFactory.GetGrain<IPartitionGrain>(replica);
            var stored = await repGrain.GetReplicaAsync(key);
            Assert.That(stored, Is.EqualTo(data));
        }
    }

    [Test]
    public async Task GetAsync_fetches_from_replicas()
    {
        var key = "key0";
        var data = new byte[] {5,4,3};
        var replicas = _provider!.GetReplicas(key).ToArray();
        var primary = replicas.First();

        var grain = _cluster!.GrainFactory.GetGrain<IPartitionGrain>(primary);
        await grain.PutAsync(key, data);

        // call from a node not containing a replica
        var other = Enumerable.Range(0,4).Select(i => $"partition-{i}").Except(replicas).First();
        var otherGrain = _cluster.GrainFactory.GetGrain<IPartitionGrain>(other);
        var value = await otherGrain.GetAsync(key);
        Assert.That(value, Is.EqualTo(data));
    }
}
