namespace Nemcache.Service.Notifications
{
    internal interface IKeyCacheNotification : ICacheNotification
    {
        string Key { get; }
    }
}