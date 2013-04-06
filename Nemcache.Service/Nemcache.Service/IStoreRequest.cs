namespace Nemcache.Service
{
    public interface IStoreRequest : IRequest
    {
        byte[] Data { get; }
    }
}