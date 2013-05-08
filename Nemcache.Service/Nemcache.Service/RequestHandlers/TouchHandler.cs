using System.Linq;
using System.Text;

namespace Nemcache.Service.RequestHandlers
{
    class TouchHandler : IRequestHandler
    {
        private readonly RequestConverters _converters;
        private readonly IMemCache _cache;

        public TouchHandler(RequestConverters converters, IMemCache cache)
        {
            _converters = converters;
            _cache = cache;
        }

        private byte[] HandleTouch(string[] commandParams)
        {
            var key = _converters.ToKey(commandParams[0]);
            var exptime = _converters.ToExpiry(commandParams[1]);
            bool success = _cache.Touch(key, exptime);
            return success
                       ? Encoding.ASCII.GetBytes("OK\r\n")
                       : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        public void HandleRequest(IRequestContext context)
        {
            var result = HandleTouch(context.Parameters.ToArray());
            context.ResponseStream.WriteAsync(result, 0, result.Length);
        }
    }
}