using System.Linq;
using System.Reactive.Concurrency;
using System.Text;

namespace Nemcache.Service.RequestHandlers
{
    internal class MutateHandler : IRequestHandler
    {
        private readonly IMemCache _cache;
        private readonly byte[] _endOfLine = new byte[] {13, 10}; // Ascii for "\r\n"
        private readonly RequestConverters _helpers;

        public MutateHandler(RequestConverters helpers, IMemCache cache, IScheduler scheduler)
        {
            _helpers = helpers;
            _cache = cache;
        }

        public void HandleRequest(IRequestContext context)
        {
            var response = HandleMutate(context.CommandName, context.Parameters.ToArray());
            context.ResponseStream.WriteAsync(response, 0, response.Length);
        }

        private byte[] HandleMutate(string commandName, string[] commandParams)
        {
            var key = _helpers.ToKey(commandParams[0]);
            var incr = ulong.Parse(commandParams[1]);
            byte[] resultData;
            bool result = _cache.Mutate(key, incr, out resultData, commandName == "incr");
            return result && resultData != null
                       ? resultData.Concat(_endOfLine).ToArray()
                       : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }
    }
}