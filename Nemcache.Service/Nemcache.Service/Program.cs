using System.IO;
using System.Net;
using System.Reactive.Concurrency;
using Topshelf;

namespace Nemcache.Service
{
    internal class Program
    {
        private static void Main()
        {
            HostFactory.Run(hc =>
                {
                    hc.Service<Service>(s =>
                        {
                            s.ConstructUsing(() => new Service());
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
            private RequestResponseTcpServer _server;

            public Service()
            {
                const int capacity = 1024*1024*100;
                var memCache = new MemCache(capacity);

                const string cacheFileName = "cache.bin";
                if (File.Exists(cacheFileName))
                {
                    RestoreCache(cacheFileName, memCache);
                }
                // Subscribing after restore has the effect of compacting the cache.
                var archiver = new StreamArchiver(File.OpenWrite(cacheFileName), memCache.Notifications);

                var requestHandler = new RequestHandler(Scheduler.Default, memCache);
                _server = new RequestResponseTcpServer(IPAddress.Any, 11222, requestHandler.Dispatch);
            }

            private static void RestoreCache(string cacheFileName, MemCache memCache)
            {
                using (var file = File.OpenRead(cacheFileName))
                {
                    StreamArchiver.Restore(file, memCache);
                }
            }

            public void Start()
            {
            }

            public void Stop()
            {
            }
        }
    }
}