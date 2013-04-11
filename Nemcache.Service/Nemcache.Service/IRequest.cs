namespace Nemcache.Service
{
    public interface IRequest
    {
        string CommandName { get; }

        string Key { get; }
        
        byte[] Data { get; }

        int Value { get; }
    }
}