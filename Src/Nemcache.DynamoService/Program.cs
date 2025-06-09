using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Hosting;
using Nemcache.Storage;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;
using Nemcache.DynamoService.Grains;
using Nemcache.DynamoService.Routing;

var host = Host.CreateDefaultBuilder(args)
    .UseOrleans(siloBuilder =>
    {
        siloBuilder.UseLocalhostClustering();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IMemCache>(sp =>
            new MemCache(1024UL * 1024 * 1024, System.Reactive.Concurrency.Scheduler.Default));

        services.AddOptions<RingProviderOptions>()
            .Configure(options =>
            {
                options.PartitionCount = 32;
                options.ReplicaCount = 3;
            });

        services.AddSingleton<RingProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RingProviderOptions>>().Value;
            return new RingProvider(opts.PartitionCount, opts.ReplicaCount);
        });

        services.AddSingleton<IFileSystem, FileSystemWrapper>();
        services.AddSingleton(sp => new StreamArchiver(
            sp.GetRequiredService<IFileSystem>(),
            "dynamo.log",
            (MemCache)sp.GetRequiredService<IMemCache>(),
            10_000));
        services.AddSingleton<ICachePersistence>(sp =>
        {
            var cache = (MemCache)sp.GetRequiredService<IMemCache>();
            var fs = sp.GetRequiredService<IFileSystem>();
            var restorer = new CacheRestorer(cache, fs, "dynamo.log");
            return new StreamPersistence(sp.GetRequiredService<StreamArchiver>(), restorer);
        });
    })
    .Build();

host.Services.GetRequiredService<ICachePersistence>().Restore();
host.Run();
