using Nemcache.Storage.Notifications;
using ProtoBuf;

namespace Nemcache.Storage.Persistence
{
    [ProtoContract]
    public class ArchiveEntry
    {
        [ProtoMember(1)]
        public StoreNotification Store { get; set; }

        [ProtoMember(2)]
        public ClearNotification Clear { get; set; }

        [ProtoMember(3)]
        public TouchNotification Touch { get; set; }

        [ProtoMember(4)]
        public RemoveNotification Remove { get; set; }
    }
}