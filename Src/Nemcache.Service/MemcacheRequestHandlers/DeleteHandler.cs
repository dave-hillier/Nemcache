using Nemcache.Storage;
﻿using System.Linq;
using System.Text;

namespace Nemcache.Service.RequestHandlers
{
    internal class DeleteHandler : IRequestHandler
    {
        private readonly IMemCache _cache;
        private readonly RequestConverters _helpers;

        public DeleteHandler(RequestConverters helpers, IMemCache cache)
        {
            _helpers = helpers;
            _cache = cache;
        }

        public void HandleRequest(IRequestContext context)
        {
            var commandParams = context.Parameters.ToArray();
            var key = _helpers.ToKey(commandParams[0]);
            var result = _cache.Remove(key)
                             ? Encoding.ASCII.GetBytes("DELETED\r\n")
                             : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
            context.ResponseStream.WriteAsync(result, 0, result.Length);
        }
    }
}