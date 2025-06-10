using Nemcache.Storage;
namespace Nemcache.DynamoService.Services
{
    public interface IMemCacheFactory
    {
        IMemCache Create();
    }

    public class DefaultMemCacheFactory : IMemCacheFactory
    {
        private readonly IMemCache _cache;

        public DefaultMemCacheFactory(IMemCache cache)
        {
            _cache = cache;
        }

        public IMemCache Create() => _cache;
    }
}
