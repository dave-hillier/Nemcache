using System.Text;

namespace Nemcache.Service
{
    class SetCommand : ICommand
    {
        private readonly IArrayCache _cache;

        public SetCommand(IArrayCache cache)
        {
            _cache = cache;
        }

        public string Name { get { return "set"; } }

        public byte[] Execute(IRequest storeRequest)
        {
            _cache.Set(storeRequest.Key, storeRequest.Data);
            return Encoding.ASCII.GetBytes("STORED\r\n");
        }
    }
}
