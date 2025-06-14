﻿using System;
using System.ComponentModel;
using ProtoBuf;

namespace Nemcache.Storage.Notifications
{
    public class RetrieveNotification : IKeyCacheNotification
    {
        public int EventId { get; set; }
        public string Key { get; set; }
    }

    [ProtoContract]
    public class StoreNotification : IKeyCacheNotification
    {
        [ProtoMember(3, IsRequired = true)]
        public byte[] Data { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public StoreOperation Operation { get; set; }

        [ProtoMember(5)]
        public DateTime Expiry { get; set; }

        // TODO: this type isnt well supported by protocol buffers - convert to unix time

        [ProtoMember(6)]
        [DefaultValue(0)]
        public ulong Flags { get; set; }

        [ProtoMember(1, IsRequired = true)]
        public int EventId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string Key { get; set; }
    }
}