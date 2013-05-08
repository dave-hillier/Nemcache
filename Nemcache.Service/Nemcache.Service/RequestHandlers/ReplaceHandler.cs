using System.Reactive.Concurrency;

namespace Nemcache.Service.RequestHandlers
{
    internal class ReplaceHandler : SetHandler
    {
        public ReplaceHandler(RequestConverters helpers, IMemCache cache, IScheduler scheduler) :
            base(helpers, cache)
        {
        }
    }
}