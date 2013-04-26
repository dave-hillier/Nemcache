namespace Nemcache.Service
{
    // TODO: Is this redundant with the observable?
    internal interface ICacheObserver
    {
        void Use(string key);
        void Remove(string key);
    }
}