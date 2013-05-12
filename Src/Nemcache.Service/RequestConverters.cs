using System;
using System.Reactive.Concurrency;

namespace Nemcache.Service
{
    internal class RequestConverters
    {
        private static readonly DateTime UnixTimeEpoc = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private readonly IScheduler _scheduler;

        public RequestConverters(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public DateTime ToExpiry(string expiry)
        {
            var expirySeconds = UInt32.Parse(expiry);
            // up to 60*60*24*30 seconds or unix time
            if (expirySeconds == 0)
                return DateTime.MaxValue;
            var start = expirySeconds < 60*60*24*30
                            ? _scheduler.Now.DateTime
                            : UnixTimeEpoc;
            return start + TimeSpan.FromSeconds(expirySeconds);
        }

        public string ToKey(string key)
        {
            if (key.Length > 250)
                throw new InvalidOperationException("Key too long");
            // TODO: no control chars
            return key;
        }

        public ulong ToFlags(string flags)
        {
            return UInt64.Parse(flags);
        }
    }
}