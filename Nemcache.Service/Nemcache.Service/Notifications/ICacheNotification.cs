using ProtoBuf;

namespace Nemcache.Service.Notifications
{
    internal interface ICacheNotification
    {
        int EventId { get; }
    }
}