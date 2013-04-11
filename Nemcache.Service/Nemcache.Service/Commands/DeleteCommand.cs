using System.Linq;
using System.Text;

namespace Nemcache.Service.Commands
{
    internal class DeleteCommand : ICommand
    {
        private readonly IArrayCache _cache;

        public DeleteCommand(IArrayCache cache)
        {
            _cache = cache;
        }

        public string Name { get { return "delete"; } }

        public byte[] Execute(IRequest request)
        {
            var key = request.Key;
            if (_cache.Get(key).Any())
            {
                _cache.Remove(key);
                return Encoding.ASCII.GetBytes("DELETED\r\n");
            }
            return Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }
    }
}