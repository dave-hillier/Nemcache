namespace Nemcache.Service
{
    public interface IArrayCache
    {
        bool Set(string key, byte[] value);
    }
}