using System.Configuration;
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
                    services.AddSingleton(new Service(capacity, port));
                    services.AddHostedService<Worker>();
                })
                .Build();

            host.Run();
        }
    }
}
