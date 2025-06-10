using Nemcache.Storage;

namespace Nemcache.DynamoService.Services
{
    public interface IMemCacheFactory
    {
        IMemCache Create();
    }
}
