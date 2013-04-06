using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nemcache.Service
{
    internal class GetCommand : ICommand
    {
        private readonly IArrayCache _cache;

        public GetCommand(IArrayCache cache)
        {
            _cache = cache;
        }

        public string Name { get { return "get"; } }

        public byte[] Execute(IRequest storeRequest)
        {
            var data = _cache.Get(storeRequest.Key);
            var result = Enumerable.Empty<byte>();
            if (data != null && data.Length > 0)
            {
                int flags = 0;
                string casUnique = "";
                var value = Encoding.ASCII.GetBytes(string.Format("VALUE {0} {1} {2}{3}\r\n", 
                    storeRequest.Key, flags, data.Length, casUnique));
                var newline = Encoding.ASCII.GetBytes("\r\n");
                result = value.Concat(data).Concat(newline);
            }
            var end = Encoding.ASCII.GetBytes("END\r\n");
            return result.Concat(end).ToArray();
        }
    }
}