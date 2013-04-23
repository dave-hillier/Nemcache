using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Topshelf;

namespace Nemcache.Service
{
    internal class Program
    {
        private static void Main(string[] args)
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
        class Service
        {
            public Service()
            {
                int capacity = 1024 * 1024 * 100;
                var requestHandler = new RequestHandler(capacity);
                var server = new RequestResponseTcpServer(IPAddress.Any, 11222, requestHandler.Dispatch);
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