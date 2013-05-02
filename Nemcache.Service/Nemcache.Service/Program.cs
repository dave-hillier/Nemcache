using System.Configuration;
using Topshelf;

namespace Nemcache.Service
{
    internal class Program
    {
        private static void Main()
        {
            var capacitySetting = ConfigurationManager.AppSettings["Capacity"];
            ulong capacity = capacitySetting != null ? ulong.Parse(capacitySetting) : 1024 * 1024 * 1024 * 4L; // 4GB

            var portSetting = ConfigurationManager.AppSettings["Port"];
            uint port = portSetting != null ? uint.Parse(portSetting) : 11222;

            var cacheFileName = ConfigurationManager.AppSettings["CacheFile"] ?? "cache.bin";

            var partitionSizeSetting = ConfigurationManager.AppSettings["Port"];
            uint partitionSize = partitionSizeSetting != null ? uint.Parse(partitionSizeSetting) : 512 * 1024 * 1024;

            HostFactory.Run(hc =>
                {
                    hc.Service<Service>(s =>
                        {
                            s.ConstructUsing(() => new Service(capacity, port, cacheFileName, partitionSize));
                            s.WhenStarted(xs => xs.Start());
                            s.WhenStopped(xs => xs.Stop());
                        });
                    hc.RunAsNetworkService();
                    hc.SetDescription("Simple .NET implementation of Memcache; an in memory key-value cache.");

                    hc.SetDisplayName("Nemcache");
                    hc.SetServiceName("Nemcache");
                    hc.SetInstanceName("Nemcache Single Instance");
                });
        }
    }
}