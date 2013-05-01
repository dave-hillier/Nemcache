using System.Configuration;
using System.IO;
using System.Net;
using System.Reactive.Concurrency;
using Nemcache.Service.FileSystem;
using Topshelf;

namespace Nemcache.Service
{
    internal class Program
    {
        private static void Main()
        {
            var capacitySetting = ConfigurationManager.AppSettings["Capacity"];
            ulong capacity = capacitySetting != null ? ulong.Parse(capacitySetting) : 1024l * 1024l * 1024l * 4; // 4GB

            var portSetting = ConfigurationManager.AppSettings["Port"];
            uint port = portSetting != null ? uint.Parse(portSetting) : 11222;

            var cacheFileName = ConfigurationManager.AppSettings["CacheFile"] ?? "cache.bin";

            HostFactory.Run(hc =>
                {
                    hc.Service<Service>(s =>
                        {
                            s.ConstructUsing(() => new Service(capacity, port, cacheFileName));
                            s.WhenStarted(xs => xs.Start());
                            s.WhenStopped(xs => xs.Stop());
                        });
                    hc.RunAsNetworkService();
                    hc.SetDescription("Simple .NET implementation of Memcache; an in memory key-value cache.");

                    // TODO: something should indicate what instance it is?
                    hc.SetDisplayName("Nemcache");
                    hc.SetServiceName("Nemcache");
                });
        }

        private class Service
        {
            private readonly RequestResponseTcpServer _server;
            private readonly MemCache _memCache;
            private StreamArchiver _archiver;
            private readonly string _cacheFileName;
            private const int _partitionSize = 512*1024*1024;

            public Service(ulong capacity, uint port, string cacheFileName)
            {
                _cacheFileName = cacheFileName;
                _memCache = new MemCache(capacity);
                var requestHandler = new RequestHandler(Scheduler.Default, _memCache);
                _server = new RequestResponseTcpServer(IPAddress.Any, port, requestHandler.Dispatch); 
            }

            public void Start()
            {
                var file = new PartitioningFileStream(
                    new FileSystemWrapper(),
                    Path.GetFileNameWithoutExtension(_cacheFileName),
                    Path.GetExtension(_cacheFileName), _partitionSize, FileAccess.ReadWrite);

                // TODO: Can Read?
                //StreamArchiver.Restore(file, _memCache);
                
                // Subscribing after restore has the effect of compacting the cache.
                _archiver = new StreamArchiver(file, _memCache.Notifications);
                
                _server.Start();
            }

            public void Stop()
            {
                _server.Stop();
                _archiver.Dispose();
            }
        }
    }
}