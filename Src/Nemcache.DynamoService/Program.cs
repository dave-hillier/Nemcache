using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Hosting;
using Nemcache.Storage;
using Nemcache.Storage.IO;
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
        // Persistence is handled per partition grain
    })
    .Build();

host.Run();
