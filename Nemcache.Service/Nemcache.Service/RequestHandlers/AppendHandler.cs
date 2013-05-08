using System.Reactive.Concurrency;

namespace Nemcache.Service.RequestHandlers
{
    internal class AppendHandler : SetHandler
    {
        public AppendHandler(RequestConverters helpers, IMemCache cache, IScheduler scheduler) :
            base(helpers, cache)
        {
        }
    }
}