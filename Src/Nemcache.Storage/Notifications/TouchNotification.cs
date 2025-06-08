using System;
using ProtoBuf;

namespace Nemcache.Storage.Notifications
{
    [ProtoContract]
    public class TouchNotification : IKeyCacheNotification
    {
        [ProtoMember(1, IsRequired = true)]
        public int EventId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string Key { get; set; }

        // TODO: this type isnt well supported by protocol buffers - convert to unix time
        [ProtoMember(3)]
        public DateTime Expiry { get; set; }
    }
}