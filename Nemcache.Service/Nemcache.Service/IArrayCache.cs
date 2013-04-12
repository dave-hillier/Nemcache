namespace Nemcache.Service
{
    public interface IArrayCache
    {
        bool Set(string key, byte[] value);

        byte[] Get(string key);

        void Remove(string key);
        byte[] Increase(string key, ulong increment);
        byte[] Decrease(string key, ulong decrement);
    }
}