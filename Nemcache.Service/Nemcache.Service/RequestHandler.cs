using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nemcache.Service
{
    internal class RequestHandler
    {
        private static readonly DateTime UnixTimeEpoc = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private readonly byte[] _endOfLine = new byte[] {13, 10}; // Ascii for "\r\n"
        private readonly MemCache _cache;

        public RequestHandler(int capacity)
        {
            _cache = new MemCache(capacity);
        }

        public IEnumerable<byte> TakeFirstLine(byte[] request)
        {
            int endOfLineIndex = -1;
            for (int i = 0; i < request.Length; ++i)
            {
                if (request[i + 0] == _endOfLine[0] &&
                    request[i + 1] == _endOfLine[1])
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

        public DateTime ToExpiry(string expiry)
        {
            var expirySeconds = uint.Parse(expiry);
            // up to 60*60*24*30 seconds or unix time
            if (expirySeconds == 0)
                return DateTime.MaxValue;
            var start = expirySeconds < 60*60*24*30
                            ? Scheduler.Current.Now
                            : UnixTimeEpoc;
            return start + TimeSpan.FromSeconds(expirySeconds);
        }

        public string ToKey(string key)
        {
            //if (key.Length > 250)
            //    throw new InvalidOperationException("Key too long");
            // TODO: no control chars
            return key;
        }

        public ulong ToFlags(string flags)
        {
            return ulong.Parse(flags);
        }

        public byte[] Dispatch(string remoteEndpoint, byte[] request, IDisposable clientConnectionHandle)
            // TODO: an interface for this?
        {
            // TODO: Is it possible for the client to send multiple requests in one.
            try
            {
                var input = TakeFirstLine(request).ToArray();
                request = request.Skip(input.Length + 2).ToArray();
                var requestFirstLine = Encoding.ASCII.GetString(input);
                var requestTokens = requestFirstLine.Split(' ');
                var commandName = requestTokens.First();
                var commandParams = requestTokens.Skip(1).ToArray();
                bool noreply = commandParams.LastOrDefault() == "noreply" && !commandName.StartsWith("get");

                var result = DispatchCommand(request, input, commandName, commandParams, clientConnectionHandle);
                return noreply ? new byte[] {} : result;
            }
            catch (Exception ex)
            {
                return Encoding.ASCII.GetBytes(string.Format("SERVER ERROR {0}\r\n", ex.Message));
            }
        }

        private byte[] DispatchCommand(byte[] request, byte[] input, string commandName, string[] commandParams,
                                       IDisposable clientConnectionHandle)
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
                    return HandleStore(request, commandName, commandParams);
                case "cas": //cas <key> <flags> <exptime> <bytes> <cas unique> [noreply]\r\n
                    return HandleCas(request, commandParams);
                case "delete": // delete <key> [noreply]\r\n
                    return HandleDelete(commandParams);
                case "incr": //incr <key> <value> [noreply]\r\n
                case "decr":
                    return HandleMutate(commandName, commandParams);
                case "touch": //touch <key> <exptime> [noreply]\r\n
                    return HandleTouch(commandParams);
                case "stats": // stats <args>\r\n or stats\r\n
                    return HandleStats(commandName, commandParams);
                case "flush_all": // [numeric] [noreply]
                    return HandleFlushAll(commandParams);
                case "quit": //     quit\r\n
                    clientConnectionHandle.Dispose();
                    return new byte[] {};
                case "version":
                    return Encoding.ASCII.GetBytes("Nemcache " +
                                                   GetType().Assembly.GetName().Version + "\r\n");
                case "exception":
                    throw new Exception("test exception");
                default:
                    return Encoding.ASCII.GetBytes("ERROR\r\n");
            }
        }

        public byte[] Store(string commandName, string key, ulong flags, DateTime exptime, byte[] data)
        {
            if (data.Length > _cache.Capacity)
            {
                return Encoding.ASCII.GetBytes("ERROR Over capacity\r\n");
            }
            bool stored;
            switch (commandName)
            {
                case "set":
                    stored = _cache.Store(key, flags, exptime, data);
                    break;
                case "replace":
                    stored = _cache.Replace(key, flags, exptime, data);
                    break;
                case "add":
                    stored = _cache.Add(key, flags, exptime, data);
                    break;
                case "append":
                case "prepend":
                    stored = _cache.Append(key, flags, exptime, data, commandName == "append");
                    break;
                default:
                    throw new InvalidOperationException(commandName);
            }
            return stored
                       ? Encoding.ASCII.GetBytes("STORED\r\n")
                       : Encoding.ASCII.GetBytes("NOT_STORED\r\n");
        }


        private byte[] HandleGet(string[] commandParams)
        {
            var keys = commandParams.Select(ToKey);

            var entries = _cache.Retrieve(keys);

            var response = from entry in entries
                           where !entry.Value.IsExpired
                           let valueText = string.Format("VALUE {0} {1} {2}{3}\r\n",
                                                         entry.Key,
                                                         entry.Value.Flags,
                                                         entry.Value.Data.Length,
                                                         entry.Value.CasUnique != 0 ? " " + entry.Value.CasUnique : "")
                           let asAscii = Encoding.ASCII.GetBytes(valueText)
                           select asAscii.Concat(entry.Value.Data).Concat(_endOfLine);

            var endOfMessage = Encoding.ASCII.GetBytes("END\r\n");
            return response.SelectMany(a => a).Concat(endOfMessage).ToArray();
        }

        private byte[] HandleTouch(string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var exptime = ToExpiry(commandParams[1]);
            bool success = _cache.Touch(key, exptime);
            return success
                       ? Encoding.ASCII.GetBytes("OK\r\n")
                       : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleMutate(string commandName, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var incr = ulong.Parse(commandParams[1]);
            byte[] resultData;
            bool result = _cache.Mutate(commandName, key, incr, out resultData);
            return result && resultData != null
                       ? resultData.Concat(_endOfLine).ToArray()
                       : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleFlushAll(string[] commandParams)
        {
            if (commandParams.Length > 0)
            {
                var delay = TimeSpan.FromSeconds(uint.Parse(commandParams[0]));
                Scheduler.Current.Schedule(delay, () => { _cache.Clear(); });
            }
            else
            {
                _cache.Clear();
            }
            return Encoding.ASCII.GetBytes("OK\r\n");
        }

        private byte[] HandleStats(string commandName, string[] commandParams)
        {
            return new byte[] {};
        }


        private byte[] HandleDelete(string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            return _cache.Remove(key)
                       ? Encoding.ASCII.GetBytes("DELETED\r\n")
                       : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleCas(byte[] request, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var flags = ToFlags(commandParams[1]);
            var exptime = ToExpiry(commandParams[2]);
            var bytes = int.Parse(commandParams[3]);
            var casUnique = ulong.Parse(commandParams[4]);
            byte[] data = request.Take(bytes).ToArray();

            return _cache.Cas(key, flags, exptime, casUnique, data)
                       ? Encoding.ASCII.GetBytes("STORED\r\n")
                       : Encoding.ASCII.GetBytes("EXISTS\r\n");
        }

        // TODO: consider wrapping all these parameters in a request type
        private byte[] HandleStore(byte[] request, string commandName, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var flags = ToFlags(commandParams[1]);
            var exptime = ToExpiry(commandParams[2]);
            var bytes = int.Parse(commandParams[3]);
            byte[] data = request.Take(bytes).ToArray();

            return Store(commandName, key, flags, exptime, data);
        }
    }
}