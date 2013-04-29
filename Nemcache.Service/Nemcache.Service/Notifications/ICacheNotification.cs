using ProtoBuf;

namespace Nemcache.Service.Notifications
{
    public interface ICacheNotification
    {
        int EventId { get; }
    }
}