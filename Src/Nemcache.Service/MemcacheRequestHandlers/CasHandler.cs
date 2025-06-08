using Nemcache.Storage;
﻿using System.Linq;
using System.Text;

namespace Nemcache.Service.RequestHandlers
{
    // TODO: dont bother with this??
    internal class CasHandler : IRequestHandler
    {
        private readonly IMemCache _cache;
        private readonly RequestConverters _helpers;

        public CasHandler(RequestConverters helpers, IMemCache cache)
        {
            _helpers = helpers;
            _cache = cache;
        }

        public void HandleRequest(IRequestContext context)
        {
            var commandParams = context.Parameters.ToArray();

            var key = _helpers.ToKey(commandParams[0]);
            var flags = _helpers.ToFlags(commandParams[1]);
            var exptime = _helpers.ToExpiry(commandParams[2]);
            var casUnique = ulong.Parse(commandParams[4]);
            var result = _cache.Cas(key, flags, exptime, casUnique, context.DataBlock)
                             ? Encoding.ASCII.GetBytes("STORED\r\n")
                             : Encoding.ASCII.GetBytes("EXISTS\r\n");

            context.ResponseStream.WriteAsync(result, 0, result.Length);
        }
    }
}