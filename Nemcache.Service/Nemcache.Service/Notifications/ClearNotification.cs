using ProtoBuf;

namespace Nemcache.Service.Notifications
{
    [ProtoContract]
    public class ClearNotification : ICacheNotification
    {
        [ProtoMember(1, IsRequired = true)]
        public int SequenceId { get; set; }
    }
}