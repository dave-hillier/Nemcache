using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nemcache.Service
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var capacityEnv = Environment.GetEnvironmentVariable("Capacity");
            ulong capacity = capacityEnv != null ? ulong.Parse(capacityEnv) : 4UL * 1024 * 1024 * 1024;

            var portEnv = Environment.GetEnvironmentVariable("Port");
            uint port = portEnv != null ? uint.Parse(portEnv) : 11222;

            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(new Service(capacity, port));
                    services.AddHostedService<HostedNemcacheService>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
