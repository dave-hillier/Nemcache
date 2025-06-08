using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Reactive.Concurrency;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;
using Nemcache.Storage;
using Nemcache.Service.RequestHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nemcache.Service
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var capacitySetting = ConfigurationManager.AppSettings["Capacity"];
            ulong capacity = capacitySetting != null ? ulong.Parse(capacitySetting) : 1024UL * 1024 * 1024 * 4;

            var portSetting = ConfigurationManager.AppSettings["Port"];
            uint port = portSetting != null ? uint.Parse(portSetting) : 11222;

            var host = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IMemCache>(sp => new MemCache(capacity, Scheduler.Default));
                    services.AddSingleton(sp =>
                    {
                        var scheduler = Scheduler.Default;
                        var cache = (IMemCache)sp.GetRequiredService<IMemCache>();
                        var handlers = Service.GetRequestHandlers(scheduler, cache);
                        return new RequestDispatcher(scheduler, cache, handlers);
                    });
                    services.AddSingleton(sp =>
                    {
                        var dispatcher = sp.GetRequiredService<RequestDispatcher>();
                        return new RequestResponseTcpServer(IPAddress.Any, (int)port, dispatcher);
                    });
                    services.AddSingleton(sp =>
                    {
                        var cache = sp.GetRequiredService<IMemCache>();
                        var handlers = new Dictionary<string, IHttpHandler>
                        {
                            {"/cache/(.+)", new CacheRestHttpHandler(cache)},
                            {"/static/(.+)", new StaticFileHttpHandler()}
                        };
                        return new CacheRestServer(handlers, new[]
                        {
                            "http://localhost:8222/cache/",
                            "http://localhost:8222/static/"
                        });
                    });
                    services.AddSingleton(sp =>
                    {
                        var cache = sp.GetRequiredService<IMemCache>();
                        return new WebSocketServer(new[] {"http://localhost:8222/sub/"}, o => new CacheEntrySubscriptionHandler(cache, o));
                    });
                    services.AddSingleton<IFileSystem, FileSystemWrapper>();
                    var useBitcask = Environment.GetEnvironmentVariable("NEMCACHE_USE_BITCASK") == "1";
                    if (useBitcask)
                    {
                        services.AddSingleton(sp =>
                        {
                            var fs = sp.GetRequiredService<IFileSystem>();
                            return new BitcaskStore(fs, "data", 10_000_000);
                        });
                        services.AddSingleton<ICachePersistence>(sp =>
                        {
                            var cache = (MemCache)sp.GetRequiredService<IMemCache>();
                            var store = sp.GetRequiredService<BitcaskStore>();
                            return new BitcaskPersistence(store, cache);
                        });
                    }
                    else
                    {
                        services.AddSingleton(sp =>
                        {
                            var cache = sp.GetRequiredService<IMemCache>();
                            var fs = sp.GetRequiredService<IFileSystem>();
                            return new CacheRestorer(cache, fs, "cachelog.bin");
                        });
                        services.AddSingleton<ICachePersistence>(sp =>
                        {
                            var fs = sp.GetRequiredService<IFileSystem>();
                            var cache = (MemCache)sp.GetRequiredService<IMemCache>();
                            var archiver = new StreamArchiver(fs, "cachelog.bin", cache, 10000);
                            var restorer = sp.GetRequiredService<CacheRestorer>();
                            return new StreamPersistence(archiver, restorer);
                        });
                    }
                    services.AddSingleton<Service>();
                    services.AddHostedService<Worker>();
                })
                .Build();

            host.Run();
        }
    }
}
