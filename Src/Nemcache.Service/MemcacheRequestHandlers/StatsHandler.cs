using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Nemcache.Storage;

namespace Nemcache.Service.RequestHandlers
{
    internal class StatsHandler : IRequestHandler
    {
        private readonly IMemCache _cache;

        public StatsHandler(IMemCache cache)
        {
            _cache = cache;
        }

        public void HandleRequest(IRequestContext context)
        {
            var args = context.Parameters.ToArray();
            string response;

            if (args.Length > 0 && args[0].Equals("settings", StringComparison.OrdinalIgnoreCase))
            {
                response = $"STAT maxbytes {_cache.Capacity}\r\nEND\r\n";
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendFormat("STAT pid {0}\r\n", Process.GetCurrentProcess().Id);
                var startTime = Process.GetCurrentProcess().StartTime.ToUniversalTime();
                var uptime = (int)(DateTime.UtcNow - startTime).TotalSeconds;
                sb.AppendFormat("STAT uptime {0}\r\n", uptime);
                sb.AppendFormat("STAT time {0}\r\n", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                sb.AppendFormat("STAT version {0}\r\n", GetType().Assembly.GetName().Version);
                sb.AppendFormat("STAT pointer_size {0}\r\n", IntPtr.Size * 8);
                sb.AppendFormat("STAT curr_items {0}\r\n", _cache.Keys.Count());
                sb.AppendFormat("STAT bytes {0}\r\n", _cache.Used);
                sb.AppendFormat("STAT limit_maxbytes {0}\r\n", _cache.Capacity);
                sb.AppendFormat("STAT threads {0}\r\n", 1);
                sb.Append("END\r\n");
                response = sb.ToString();
            }

            var bytes = Encoding.ASCII.GetBytes(response);
            context.ResponseStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}

