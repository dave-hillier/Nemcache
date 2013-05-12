using ProtoBuf;

namespace Nemcache.Service.Notifications
{
    [ProtoContract]
    public class RemoveNotification : IKeyCacheNotification
    {
        [ProtoMember(1, IsRequired = true)]
        public int EventId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string Key { get; set; }
    }
}