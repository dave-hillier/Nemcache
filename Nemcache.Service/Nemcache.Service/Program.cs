using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Text;
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

                string inFile, outFile;
                var files = Directory.GetFiles(".", "*.cachebin");
                if (files.Any())
                {
                    var ordered = files.OrderByDescending(f => int.Parse(Path.GetFileNameWithoutExtension(f))).ToArray();
                    inFile = ordered.First();
                    outFile = (int.Parse(Path.GetFileNameWithoutExtension(inFile)) + 1).ToString() + ".cachebin";

                    // TODO: delete the rest
                }
                else
                {
                    inFile = "not.cachebin";
                    outFile = "1.cachebin";
                }

                if (File.Exists(inFile))
                {
                    using (var file = File.OpenRead(inFile))
                    {
                        StreamArchiver.Restore(file, memCache);
                    }
                }

                // This has the effect of compacting the cache
                var archiver = new StreamArchiver(File.OpenWrite(outFile), memCache.Notifications);

                var requestHandler = new RequestHandler(Scheduler.Default, memCache);
                _server = new RequestResponseTcpServer(IPAddress.Any, 11222, requestHandler.Dispatch);
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