namespace Nemcache.Service
{
    public interface IRequest
    {
        string CommandName { get; }
        string Key { get; }
    }
}