namespace Nemcache.Storage.Notifications
{
    public interface IKeyCacheNotification : ICacheNotification
    {
        string Key { get; }
    }
}
