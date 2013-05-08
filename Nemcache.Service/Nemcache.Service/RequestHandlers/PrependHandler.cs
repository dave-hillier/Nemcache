using System.Reactive.Concurrency;

namespace Nemcache.Service.RequestHandlers
{
    internal class PrependHandler : SetHandler
    {
        public PrependHandler(RequestConverters helpers, IMemCache cache, IScheduler scheduler) :
            base(helpers, cache)
        {
        }
    }
}