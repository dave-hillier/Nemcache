using System;
using System.Reactive.Concurrency;

namespace Nemcache.Service
{
    public struct CacheEntry
    {
        public ulong Flags { get; set; }
        public DateTime Expiry { get; set; }
        public ulong CasUnique { get; set; }
        public byte[] Data { get; set; }
        public int EventId { get; set; }

        public bool IsExpired(IScheduler scheduler)
        {
            return Expiry < scheduler.Now;
        }
    }
}