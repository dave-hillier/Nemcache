namespace Nemcache.Service
{
    public interface IRequest
    {
        string Command { get; }

        string Key { get; }

        byte[] Data { get; }
    }
}