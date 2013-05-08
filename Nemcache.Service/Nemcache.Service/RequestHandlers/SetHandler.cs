using System;
using System.Linq;
using System.Text;

namespace Nemcache.Service.RequestHandlers
{
    internal class SetHandler : IRequestHandler
    {
        private readonly IMemCache _cache;
        private readonly RequestConverters _helpers;

        public SetHandler(RequestConverters helpers, IMemCache cache)
        {
            _helpers = helpers;
            _cache = cache;
        }

        public void HandleRequest(IRequestContext context)
        {
            var @params = context.Parameters.ToArray();
            var key = _helpers.ToKey(@params[0]);
            var flags = _helpers.ToFlags(@params[1]);
            var exptime = _helpers.ToExpiry(@params[2]);
            var response = Store(context.CommandName, key, flags, exptime, context.DataBlock);
            context.ResponseStream.WriteAsync(response, 0, response.Length);
        }

        public byte[] Store(string commandName, string key, ulong flags, DateTime exptime, byte[] data)
        {
            if ((ulong) data.Length > _cache.Capacity)
            {
                return Encoding.ASCII.GetBytes("ERROR Over capacity\r\n");
            }
            bool stored = false;
            switch (commandName)
            {
                case "set":
                    stored = _cache.Store(key, flags, data, exptime);
                    break;
                case "replace":
                    stored = _cache.Replace(key, flags, exptime, data);
                    break;
                case "add":
                    stored = _cache.Add(key, flags, exptime, data);
                    break;
                case "append":
                case "prepend":
                    stored = _cache.Append(key, flags, exptime, data, commandName == "append");
                    break;
            }
            return stored
                       ? Encoding.ASCII.GetBytes("STORED\r\n")
                       : Encoding.ASCII.GetBytes("NOT_STORED\r\n");
        }
    }
}