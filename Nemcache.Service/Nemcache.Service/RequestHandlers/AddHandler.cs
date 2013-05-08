using System.Reactive.Concurrency;

namespace Nemcache.Service.RequestHandlers
{
    internal class AddHandler : SetHandler
    {
        public AddHandler(RequestConverters helpers, IMemCache cache, IScheduler scheduler) :
            base(helpers, cache)
        {
        }
    }
}