using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    internal class RequestHandler
    {
        private readonly byte[] EndOfLine = new byte[] { 13, 10 }; // Ascii for "\r\n"
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        private Random _rng = new Random();

        private struct CacheEntry
        {
            public ulong Flags { get; set; }
            public DateTime Expiry { get; set; }
            public ulong CasUnique { get; set; }
            public byte[] Data { get; set; }

            public bool IsExpired { get { return Expiry < Scheduler.Current.Now; } }
        }

        public RequestHandler(int capacity)
        {
            Capacity = capacity;
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
            var start = expirySeconds < 60 * 60 * 24 * 30 ? Scheduler.Current.Now : UnixTimeEpoc;
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
                bool noreply = commandParams.LastOrDefault() == "noreply";
                var result = HandleCommand(request, input, commandName, commandParams);
                return noreply ? new byte[] { } : result;
            }
            catch (Exception ex)
            {
                return Encoding.ASCII.GetBytes(string.Format("SERVER_ERROR {0}\r\n", ex.Message));
            }
        }

        private byte[] HandleCommand(byte[] request, byte[] input, string commandName, string[] commandParams)
        {
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
                    return HandleStats(commandName, commandParams);
                case "flush_all": // [numeric] [noreply]
                    return HandleFlushAll(commandParams);
                case "quit": //     quit\r\n
                    return new byte[] { };
                default:
                    return Encoding.ASCII.GetBytes("ERROR\r\n");
            }
        }

        private byte[] HandleStats(string commandName, string[] commandParams)
        {
            return new byte[] { };
        }

        private byte[] HandleFlushAll(string[] commandParams)
        {
            if (commandParams.Length > 0)
            {
                var delay = TimeSpan.FromSeconds(uint.Parse(commandParams[0]));
                Scheduler.Current.Schedule(delay, () => _cache.Clear());
            }
            else
            {
                _cache.Clear();
            }
            return Encoding.ASCII.GetBytes("OK\r\n");
        }

        private byte[] HandleTouch(string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var exptime = ToExpiry(commandParams[1]);
            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                CacheEntry touched = entry;
                touched.Expiry = exptime;
                _cache.TryUpdate(key, touched, entry); // OK to fail if something else has updated.
                return Encoding.ASCII.GetBytes("OK\r\n");
            }
            return Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleIncr(string commandName, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var incr = ulong.Parse(commandParams[1]);

            byte[] resultData = null;
            var result = _cache.TryUpdate(key,
                entry =>
                {
                    var value = ulong.Parse(Encoding.ASCII.GetString(entry.Data));
                    if (commandName == "incr")
                        value += incr;
                    else
                        value -= incr;
                    resultData = Encoding.ASCII.GetBytes(value.ToString());
                    entry.Data = resultData;
                    return entry;
                });
            return result && resultData != null ? resultData.Concat(EndOfLine).ToArray()
                : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleDelete(string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            CacheEntry entry;
            return _cache.TryRemove(key, out entry) ? Encoding.ASCII.GetBytes("DELETED\r\n") :
                Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleCas(byte[] request, byte[] input, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var flags = ToFlags(commandParams[1]);
            var exptime = ToExpiry(commandParams[2]);
            var bytes = int.Parse(commandParams[3]);
            var casUnique = ulong.Parse(commandParams[4]);
            byte[] data = request.Skip(input.Length + 2).Take(bytes).ToArray();
            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                if (entry.CasUnique == casUnique)
                {
                    var spaceRequired = Math.Abs(data.Length - entry.Data.Length);
                    if (spaceRequired > 0)
                        MakeSpaceForData(spaceRequired);
                    var newValue = new CacheEntry { CasUnique = casUnique, Data = data, Expiry = exptime, Flags = flags };
                    var stored = _cache.TryUpdate(key, newValue, entry);
                    return stored ? Encoding.ASCII.GetBytes("STORED\r\n") : 
                        Encoding.ASCII.GetBytes("EXISTS\r\n");
                }
                else
                {
                    return Encoding.ASCII.GetBytes("EXISTS\r\n");
                }
            }

            _cache[key] = new CacheEntry { CasUnique = casUnique, Data = data, Expiry = exptime, Flags = flags };
            return Encoding.ASCII.GetBytes("STORED\r\n");
        }

        // TODO: consider wrapping all these parameters in a request type
        private byte[] HandleStore(byte[] request, byte[] input, string commandName, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var flags = ToFlags(commandParams[1]);
            var exptime = ToExpiry(commandParams[2]);
            var bytes = int.Parse(commandParams[3]);
            byte[] data = request.Skip(input.Length + 2).Take(bytes).ToArray();

            if (data.Length > Capacity)
            {
                return Encoding.ASCII.GetBytes("ERROR Over capacity\r\n");
            }

            switch (commandName)
            {
                case "set":
                    return Store(key, flags, exptime, data);
                case "replace":
                    return Replace(key, flags, exptime, data);
                case "add":
                    return Add(key, flags, exptime, data);
                case "append":
                case "prepend":
                    return Append(key, flags, exptime, data, commandName == "append");
            }
            return Encoding.ASCII.GetBytes("ERROR\r\n");
        }

        private byte[] Replace(string key, ulong flags, DateTime exptime, byte[] data)
        {
            MakeSpaceForData(data.Length); // TODO: current value could reduce this requirement 
            var exists = _cache.TryUpdate(key, e =>
            {
                return new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
            });
            return exists ? Encoding.ASCII.GetBytes("STORED\r\n") :
                Encoding.ASCII.GetBytes("NOT_STORED\r\n");
        }

        private byte[] Add(string key, ulong flags, DateTime exptime, byte[] data)
        {
            MakeSpaceForData(data.Length);
            var entry = new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
            var result = _cache.TryAdd(key, entry);
            return result ? Encoding.ASCII.GetBytes("STORED\r\n") :
                Encoding.ASCII.GetBytes("NOT_STORED\r\n");
        }

        private byte[] Append(string key, ulong flags, DateTime exptime, byte[] data, bool prepend)
        {
            MakeSpaceForData(data.Length);
            var exists = _cache.TryUpdate(key, e =>
            {
                // TODO: ensure that this entry is not becomming bigger than capacity?
                var newEntry = e;
                newEntry.Data = prepend ?
                    e.Data.Concat(data).ToArray() :
                    data.Concat(e.Data).ToArray();
                return newEntry;
            });
            return !exists ? Store(key, flags, exptime, data) : Encoding.ASCII.GetBytes("STORED\r\n");
        }

        private byte[] Store(string key, ulong flags, DateTime exptime, byte[] data)
        {
            MakeSpaceForData(data.Length);
            _cache[key] = new CacheEntry { Data = data, Expiry = exptime, Flags = flags };
            return Encoding.ASCII.GetBytes("STORED\r\n");
        }

        private void MakeSpaceForData(int length)
        {
            while (Used + length > Capacity)
            {
                RemoveRandomEntry();
            }
        }

        private void RemoveRandomEntry()
        {
            var keyToEvict = _cache.Keys.ElementAt(_rng.Next(0, _cache.Keys.Count));
            CacheEntry entry;
            _cache.TryRemove(keyToEvict, out entry);
        }        

        private byte[] HandleGet(string[] commandParams)
        {
            var keys = commandParams.Select(ToKey);

            var entries = from key in keys
                          where _cache.ContainsKey(key)
                          select new { Key = key, CacheEntry = _cache[key] };

            var response = from entry in entries
                           where !entry.CacheEntry.IsExpired
                           let valueText = string.Format("VALUE {0} {1} {2}{3}\r\n",
                               entry.Key,
                               entry.CacheEntry.Flags,
                               entry.CacheEntry.Data.Length,
                               entry.CacheEntry.CasUnique != 0 ? " " + entry.CacheEntry.CasUnique : "")
                           let asAscii = Encoding.ASCII.GetBytes(valueText)
                           select asAscii.Concat(entry.CacheEntry.Data).Concat(EndOfLine);

            var endOfMessage = Encoding.ASCII.GetBytes("END");
            return response.SelectMany(a => a).Concat(endOfMessage).Concat(EndOfLine).ToArray();
        }

        public int Capacity { get; set; }

        public int Used
        {
            get
            {
                return _cache.Values.Select(e => e.Data.Length).Sum();
            }
        }
    }


    static class ConcurrentDictionaryHelpers
    {
        public static bool TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, 
            TKey key, Func<TValue, TValue> updateFactory)
        {
            TValue curValue;
            while(dict.TryGetValue(key, out curValue))
            {
                if(dict.TryUpdate(key, updateFactory(curValue), curValue))
                    return true;
                //if we're looping either the key was removed by another thread, or another thread
                //changed the value, so we start again.
            }
            return false;
        }

        public static bool TryUpdateOptimisitic<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, 
            TKey key, Func<TValue, TValue> updateFactory)
        {
            TValue curValue;
            if (!dict.TryGetValue(key, out curValue))
                return false;
            dict.TryUpdate(key, updateFactory(curValue), curValue);
            return true;
        }
    }
}
