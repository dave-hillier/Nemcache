using ProtoBuf;

namespace Nemcache.Storage.Notifications
{
    [ProtoContract]
    public class ClearNotification : ICacheNotification
    {
        [ProtoMember(1, IsRequired = true)]
        public int EventId { get; set; }
    }
}