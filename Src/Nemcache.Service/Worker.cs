using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Nemcache.Service
{
    internal class Worker : IHostedService
    {
        private readonly Service _service;

        public Worker(Service service)
        {
            _service = service;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _service.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _service.Stop();
            return Task.CompletedTask;
        }
    }
}
