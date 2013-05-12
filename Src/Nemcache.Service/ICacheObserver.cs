namespace Nemcache.Service
{
    // TODO: add an observable for keys used. use the notifications for when keys are removed.
    internal interface ICacheObserver
    {
        void Use(string key);
        void Remove(string key);
    }
}