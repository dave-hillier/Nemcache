using System.Linq;
using System.Reactive.Concurrency;
using System.Text;

namespace Nemcache.Service.RequestHandlers
{
    internal class GetHandler : IRequestHandler
    {
        private readonly RequestConverters _helpers;
        private readonly IMemCache _cache;
        private readonly IScheduler _scheduler;

        public GetHandler(RequestConverters helpers, IMemCache cache, IScheduler scheduler)
        {
            _helpers = helpers;
            _cache = cache;
            _scheduler = scheduler;
        }

        public void HandleRequest(IRequestContext context)
        {
            var keys = context.Parameters.Select(_helpers.ToKey);

            var entries = _cache.Retrieve(keys);
            var endOfLine = Encoding.ASCII.GetBytes("\r\n");

            var response = from entry in entries
                           where !entry.Value.IsExpired(_scheduler)
                           let valueText = string.Format("VALUE {0} {1} {2}{3}\r\n",
                                                         entry.Key,
                                                         entry.Value.Flags,
                                                         entry.Value.Data.Length,
                                                         entry.Value.CasUnique != 0 ? " " + entry.Value.CasUnique : "")
                           let asAscii = Encoding.ASCII.GetBytes(valueText)
                           select asAscii.Concat(entry.Value.Data).Concat(endOfLine);

            var endOfMessage = Encoding.ASCII.GetBytes("END\r\n");

            var responseBuffer = response.SelectMany(a => a).Concat(endOfMessage).ToArray();
            context.ResponseStream.Write(responseBuffer, 0, responseBuffer.Length);
        }
    }
}