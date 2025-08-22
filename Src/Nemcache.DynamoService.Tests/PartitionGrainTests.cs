using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans.Hosting;
using Orleans.TestingHost;
using Orleans.Runtime;
using Nemcache.DynamoService.Grains;
using Nemcache.DynamoService.Routing;
using Nemcache.Storage;
using Nemcache.DynamoService.Services;
using Nemcache.Storage.IO;
using System.Reactive.Concurrency;
using System.IO;
using System;

namespace Nemcache.DynamoService.Tests;

[TestFixture]
public class PartitionGrainTests
{
    private TestCluster? _cluster;
    private RingProvider? _provider;

    [OneTimeSetUp]
    public void SetUp()
    {
        var builder = new TestClusterBuilder(2);
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

    private class SharedFileSystem : IFileSystem, IFile
    {
        public IFile File => this;

        public Stream Open(string path, FileMode mode, FileAccess access)
            => new FileStream(path, mode, access, FileShare.ReadWrite);

        public bool Exists(string path) => System.IO.File.Exists(path);
        public long Size(string filename) => new FileInfo(filename).Length;
        public void Delete(string path) => System.IO.File.Delete(path);
        public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
            => System.IO.File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
    }

    private class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IMemCacheFactory>(sp => new MemCacheFactory(1024UL * 1024, Scheduler.Default));
                services.AddSingleton<IFileSystem, SharedFileSystem>();
                services.AddSingleton(new RingProvider(partitionCount: 4, replicaCount: 3));
            });
            siloBuilder.AddIncomingGrainCallFilter<FailingPutReplicaFilter>();
        }
    }

    private class FailingPutReplicaFilter : IIncomingGrainCallFilter
    {
        public static string? OfflinePartition { get; set; }

        public Task Invoke(IIncomingGrainCallContext context)
        {
            if (OfflinePartition != null &&
                context.Grain is PartitionGrain pg &&
                context.InterfaceMethod.Name == nameof(IPartitionGrain.PutReplicaAsync) &&
                pg.GetPrimaryKeyString() == OfflinePartition)
            {
                throw new Exception("Replica offline");
            }

            return context.Invoke();
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

    [Test]
    public async Task PutAsync_recovers_when_replica_offline()
    {
        var key = "offline-key";
        var data = new byte[] { 9 };
        var replicas = _provider!.GetReplicas(key).ToArray();
        var primary = replicas.First();
        var offline = replicas.Skip(1).First();

        FailingPutReplicaFilter.OfflinePartition = offline;

        var grain = _cluster!.GrainFactory.GetGrain<IPartitionGrain>(primary);
        Assert.ThrowsAsync<Exception>(() => grain.PutAsync(key, data));

        FailingPutReplicaFilter.OfflinePartition = null;

        var offlineGrain = _cluster.GrainFactory.GetGrain<IPartitionGrain>(offline);
        var repaired = await offlineGrain.GetAsync(key);
        Assert.That(repaired, Is.EqualTo(data));

        var stored = await offlineGrain.GetReplicaAsync(key);
        Assert.That(stored, Is.EqualTo(data));
    }

    [Test]
    public async Task PutAsync_concurrent_calls_consistent()
    {
        var key1 = "concurrent1";
        var data1 = new byte[] { 1 };
        var replicas1 = _provider!.GetReplicas(key1).ToArray();
        var primary = replicas1.First();

        var key2 = "concurrent2";
        while (_provider.GetReplicas(key2).First() != primary)
        {
            key2 += "x";
        }

        var data2 = new byte[] { 2 };
        var replicas2 = _provider.GetReplicas(key2).ToArray();

        var grain = _cluster!.GrainFactory.GetGrain<IPartitionGrain>(primary);

        await Task.WhenAll(grain.PutAsync(key1, data1), grain.PutAsync(key2, data2));

        foreach (var r in replicas1)
        {
            var g = _cluster.GrainFactory.GetGrain<IPartitionGrain>(r);
            var stored = await g.GetReplicaAsync(key1);
            Assert.That(stored, Is.EqualTo(data1));
        }

        foreach (var r in replicas2)
        {
            var g = _cluster.GrainFactory.GetGrain<IPartitionGrain>(r);
            var stored = await g.GetReplicaAsync(key2);
            Assert.That(stored, Is.EqualTo(data2));
        }
    }

    [Test]
    public async Task PutAsync_handles_membership_change()
    {
        var key = "membership-key";
        var data = new byte[] { 7 };
        var replicas = _provider!.GetReplicas(key).ToArray();
        var primary = replicas.First();

        var grain = _cluster!.GrainFactory.GetGrain<IPartitionGrain>(primary);
        var putTask = grain.PutAsync(key, data);
        _cluster.StartAdditionalSilo();
        try
        {
            await putTask;
            Assert.Fail("Expected timeout");
        }
        catch (TimeoutException)
        {
            // expected
        }
    }
}
