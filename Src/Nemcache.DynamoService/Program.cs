using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Nemcache.Storage;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;
using Nemcache.DynamoService.Grains;
using Nemcache.DynamoService.Routing;
using Nemcache.DynamoService.Services;

var host = Host.CreateDefaultBuilder(args)
    .UseOrleans(siloBuilder =>
    {
        siloBuilder.UseLocalhostClustering();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IMemCacheFactory>(sp =>
            new MemCacheFactory(1024UL * 1024 * 1024,
                System.Reactive.Concurrency.Scheduler.Default));
        services.AddSingleton(new RingProvider(partitionCount: 32, replicaCount: 3));
        services.AddSingleton<IFileSystem, FileSystemWrapper>();
        // Persistence is handled per partition grain
    })
    .Build();

host.Run();
