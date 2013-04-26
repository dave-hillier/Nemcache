using System;

namespace Nemcache.Service.Notifications
{
    internal class Store : IKeyCacheNotification
    {
        public byte[] Data { get; set; }

        public DateTime Expiry { get; set; }

        public StoreOperation Operation { get; set; }

        public ulong Flags { get; set; }
        public string Key { get; set; }

        public int SequenceId { get; set; }
    }
}