using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Nemcache.Service
{
    internal class Program
    {
        private readonly byte[] EndOfLine = new byte[] { 13, 10 }; //Encoding.ASCII.GetBytes("\r\n");
        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();

        private struct CacheEntry
        {
            public ulong Flags { get; set; }
            public DateTime Expiry { get; set; }
            public ulong CasUnique { get; set; }
            public byte[] Data { get; set; }

            public bool IsExpired
            {
                get
                {
                    return Expiry < DateTime.UtcNow;
                }
            }
        }

        private static void Main(string[] args)
        {
            var p = new Program();
            var server = new RequestResponseTcpServer(IPAddress.Any, 11222, p.Dispatch);
            Console.ReadLine();
        }

        public IEnumerable<byte> TakeFirstLine(byte[] request)
        {
            int endOfLineIndex = -1;
            for (int i = 0; i < request.Length; ++i)
            {
                if (request[i + 0] == EndOfLine[0] &&
                    request[i + 1] == EndOfLine[1])
                {
                    endOfLineIndex = i;
                    break;
                }
            }
            if (endOfLineIndex != -1)
                return request.Take(endOfLineIndex);
            else
                throw new Exception("New line not found"); // TODO: better exception type.
        }

        static DateTime UnixTimeEpoc = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public DateTime ToExpiry(string expiry)
        {
            var expirySeconds = uint.Parse(expiry);
            // up to 60*60*24*30 seconds or unix time
            if (expirySeconds == 0)
                return DateTime.MaxValue;
            return (expirySeconds < 60 * 60 * 24 * 30 ? DateTime.UtcNow : UnixTimeEpoc) + TimeSpan.FromSeconds(expirySeconds);
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
            return ulong.Parse(flags);
        }

        public byte[] Dispatch(string remoteEndpoint, byte[] request)
        {
            try
            {
                var input = TakeFirstLine(request).ToArray();
                var requestFirstLine = Encoding.ASCII.GetString(input);
                var requestTokens = requestFirstLine.Split(' ');
                var commandName = requestTokens.First();
                var commandParams = requestTokens.Skip(1).ToArray();

                switch (commandName)
                {
                    case "get":
                    case "gets": // <key>*
                        return HandleGet(commandParams);
                        
                    case "add":
                    case "replace":
                    case "append":
                    case "prepend":
                    case "set": // <command name> <key> <flags> <exptime> <bytes> [noreply]
                        return HandleStore(request, input, commandName, commandParams);

                    case "cas"://cas <key> <flags> <exptime> <bytes> <cas unique> [noreply]\r\n
                        return HandleCas(request, input, commandParams);

                    case "delete": // delete <key> [noreply]\r\n
                        return HandleDelete(commandParams);

                    case "incr"://incr <key> <value> [noreply]\r\n
                    case "decr":
                        return HandleIncr(commandName, commandParams);

                    case "touch"://touch <key> <exptime> [noreply]\r\n
                        return HandleTouch(commandParams);

                    case "stats":// stats <args>\r\n or stats\r\n
                        return new byte[] { };

                    case "flush_all": // [numeric] [noreply]
                        return new byte[] { };

                    case "quit": //     quit\r\n
                        return new byte[] { };

                    default:
                        return Encoding.ASCII.GetBytes("ERROR\r\n");
                }
            }
            catch (Exception ex)
            {
                return Encoding.ASCII.GetBytes(string.Format("SERVER_ERROR {0}\r\n", ex.Message));
            }
        }

        private byte[] HandleTouch(string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var exptime = ToExpiry(commandParams[1]);
            bool noreply = commandParams.Length == 3 && commandParams[2] == "noreply";
            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                entry.Expiry = exptime;
                _cache[key] = entry;
                byte[] result = new byte[] { };
                return noreply ? new byte[] { } : result.Concat(EndOfLine).ToArray();
            }
            return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleIncr(string commandName, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var incr = ulong.Parse(commandParams[1]);
            bool noreply = commandParams.Length == 3 && commandParams[2] == "noreply";

            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                var value = ulong.Parse(Encoding.ASCII.GetString(entry.Data));
                if (commandName == "incr")
                    value += incr;
                else
                    value -= incr;
                var result = Encoding.ASCII.GetBytes(value.ToString());
                entry.Data = result;
                _cache[key] = entry;
                return noreply ? new byte[] { } : result.Concat(EndOfLine).ToArray();
            }

            return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleDelete(string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            bool noreply = commandParams.Length == 2 && commandParams[1] == "noreply";

            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                _cache.Remove(key);
                return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("DELETED\r\n");
            }

            return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleCas(byte[] request, byte[] input, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var flags = ToFlags(commandParams[1]);
            var exptime = ToExpiry(commandParams[2]);
            var bytes = int.Parse(commandParams[3]);
            var casUnique = ulong.Parse(commandParams[4]);
            bool noreply = commandParams.Length == 6 && commandParams[5] == "noreply";
            byte[] data = request.Skip(input.Length + 2).Take(bytes).ToArray();
            return new byte[] { };
        }

        private byte[] HandleStore(byte[] request, byte[] input, string commandName, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var flags = ToFlags(commandParams[1]);
            var exptime = ToExpiry(commandParams[2]);
            var bytes = int.Parse(commandParams[3]);
            bool noreply = commandParams.Length == 5 && commandParams[4] == "noreply";
            byte[] data = request.Skip(input.Length + 2).Take(bytes).ToArray();

            CacheEntry entry;
            switch (commandName)
            {
                case "set":
                    entry = new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
                    _cache[key] = entry;
                    return noreply ? new byte[] {} : Encoding.ASCII.GetBytes("STORED\r\n");
                case "replace":
                    if (_cache.TryGetValue(key, out entry))
                    {
                        entry = new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
                        _cache[key] = entry;
                        return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("STORED\r\n");
                    }
                    return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("NOT_STORED\r\n");
                case "add":
                    if (!_cache.TryGetValue(key, out entry))
                    {
                        entry = new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
                        _cache[key] = entry;
                        return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("STORED\r\n");
                    }
                    return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("NOT_STORED\r\n");

                case "append":
                case "prepend":
                    if (_cache.TryGetValue(key, out entry))
                    {
                        var newData = commandName == "append" ? 
                            entry.Data.Concat(data) :
                            data.Concat(entry.Data);
                        var newEntry = new CacheEntry {
                            Data = newData.ToArray(), 
                            Expiry = entry.Expiry,
                            Flags = entry.Flags
                        };
                        _cache[key] = newEntry;
                        return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("STORED\r\n");
                    }
                    else
                    {
                        entry = new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
                        _cache[key] = entry;
                        return noreply ? new byte[] { } : Encoding.ASCII.GetBytes("STORED\r\n");
                    }
            }

            return Encoding.ASCII.GetBytes("ERROR\r\n");
        }

        private byte[] HandleGet(string[] commandParams)
        {
            var keys = commandParams.Select(ToKey);
            
            var entries = from key in keys 
                          where _cache.ContainsKey(key) 
                          select new { Key = key, CacheEntry = _cache[key] };

            var response = from entry in entries
                           where !entry.CacheEntry.IsExpired
                           let valueText = string.Format(
                               "VALUE {0} {1} {2}{3}\r\n", 
                               entry.Key, 
                               entry.CacheEntry.Flags, 
                               entry.CacheEntry.Data.Length,
                               entry.CacheEntry.CasUnique != 0 ? " " + entry.CacheEntry.CasUnique : "")
                           let asAscii = Encoding.ASCII.GetBytes(valueText)
                           select asAscii.Concat(entry.CacheEntry.Data).Concat(EndOfLine);

	        var endOfMessage = Encoding.ASCII.GetBytes("END\r\n");

            return response.SelectMany(a => a).Concat(endOfMessage).ToArray();
        }
    }
}