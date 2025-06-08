using Nemcache.Storage;
﻿using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;

namespace Nemcache.Service.RequestHandlers
{
    internal class FlushHandler : IRequestHandler
    {
        private readonly IMemCache _cache;
        private readonly IScheduler _scheduler;

        public FlushHandler(IMemCache cache, IScheduler scheduler)
        {
            _cache = cache;
            _scheduler = scheduler;
        }

        public void HandleRequest(IRequestContext context)
        {
            var result = HandleFlushAll(context.Parameters.ToArray());
            context.ResponseStream.WriteAsync(result, 0, result.Length);
        }

        private byte[] HandleFlushAll(string[] commandParams)
        {
            if (commandParams.Length > 0)
            {
                var delay = TimeSpan.FromSeconds(uint.Parse(commandParams[0]));
                _scheduler.Schedule(delay, () => _cache.Clear());
            }
            else
            {
                _cache.Clear();
            }
            return Encoding.ASCII.GetBytes("OK\r\n");
        }
    }
}