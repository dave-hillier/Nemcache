using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class SetCommand
    {
        private readonly IArrayCache _cache;

        public SetCommand(IArrayCache cache)
        {
            _cache = cache;
        }

        public string Name { get { return "set"; } }

        public byte[] Execute(IRequest request)
        {
            _cache.Set(request.Key, request.Data);
            return Encoding.ASCII.GetBytes("STORED\r\n");
        }
    }
}
