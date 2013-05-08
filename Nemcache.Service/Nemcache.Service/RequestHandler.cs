using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    internal class RequestHandler
    {
        private static readonly DateTime UnixTimeEpoc = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private readonly byte[] _endOfLine = new byte[] {13, 10}; // Ascii for "\r\n"
        private readonly IMemCache _cache;
        private readonly IScheduler _scheduler;
        private const int RequestSizeLimit = 1024;

        public RequestHandler(IScheduler scheduler, IMemCache cache)
        {
            _scheduler = scheduler;
            _cache = cache;
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
            throw new Exception("New line not found"); // TODO: better exception type.
        }

        public DateTime ToExpiry(string expiry)
        {
            var expirySeconds = uint.Parse(expiry);
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
            return ulong.Parse(flags);
        }

        public async Task<byte[]> Dispatch(Stream stream, string remoteEndpoint, IDisposable clientConnectionHandle)
            // TODO: an interface for this?
        {
            try
            {
                return await ProcessRequest(stream, clientConnectionHandle);
            }
            catch (Exception ex)
            {
                return Encoding.ASCII.GetBytes(string.Format("SERVER ERROR {0}\r\n", ex.Message));
            }
        }

        private async Task<byte[]> ProcessRequest(Stream stream, IDisposable clientConnectionHandle)
        {
            var buffer = GetFirstLine(stream);

            var requestFirstLine = Encoding.ASCII.GetString(buffer.ToArray()).TrimEnd();

            var requestTokens = requestFirstLine.Split(' ');
            var commandName = requestTokens.First();
            var commandParams = requestTokens.Skip(1).ToArray();

            byte[] dataBlock = await GetDataBlock(commandName, stream, commandParams);

            var result = DispatchCommand(dataBlock, commandName, commandParams, clientConnectionHandle);

            return IsNoReply(commandParams, commandName) ? new byte[] {} : result;
        }

        private static bool IsNoReply(IEnumerable<string> commandParams, string commandName)
        {
            return commandParams.LastOrDefault() == "noreply" && !commandName.StartsWith("get");
        }

        private static async Task<byte[]> GetDataBlock(string commandName, Stream stream, string[] commandParams)
        {
            byte[] dataBlock = null;
            bool hasDataBlock = IsSetCommand(commandName);
            if (hasDataBlock)
            {
                var bytes = commandParams[3];
                dataBlock = new byte[Int32.Parse(bytes)];
                int count = await stream.ReadAsync(dataBlock, 0, dataBlock.Length); // TODO: does this need to repeat for large payloads?
            }
            return dataBlock;
        }

        private static bool IsSetCommand(string commandName)
        {
            return commandName == "add" ||
                   commandName == "replace" ||
                   commandName == "set" ||
                   commandName == "append" ||
                   commandName == "prepend" ||
                   commandName == "cas";
        }

        private List<byte> GetFirstLine(Stream stream)
        {
            var buffer = new List<byte>();
            byte last = 0;
            while (true)
            {
                var current = (byte)stream.ReadByte();

                buffer.Add(current);
                if (buffer.Count > RequestSizeLimit)
                    throw new Exception("New line not found");
                if (last == _endOfLine[0] &&
                    current == _endOfLine[1])
                {
                    break;
                }
                last = current;
            }
            return buffer;
        }

        private byte[] DispatchCommand(byte[] dataBlock, string commandName, string[] commandParams, IDisposable clientConnectionHandle)
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
                    return HandleStore(dataBlock, commandName, commandParams);
                case "cas": //cas <key> <flags> <exptime> <bytes> <cas unique> [noreply]\r\n
                    return HandleCas(dataBlock, commandParams);
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

        private byte[] Store(string commandName, string key, ulong flags, DateTime exptime, byte[] data)
        {
            if ((ulong)data.Length > _cache.Capacity)
            {
                return Encoding.ASCII.GetBytes("ERROR Over capacity\r\n");
            }
            bool stored = false;
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
            }
            return stored
                       ? Encoding.ASCII.GetBytes("STORED\r\n")
                       : Encoding.ASCII.GetBytes("NOT_STORED\r\n");
        }


        private byte[] HandleGet(IEnumerable<string> commandParams)
        {
            var keys = commandParams.Select(ToKey);

            var entries = _cache.Retrieve(keys);

            var response = from entry in entries
                           where !entry.Value.IsExpired(_scheduler)
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
            bool result = _cache.Mutate(key, incr, out resultData, commandName == "incr");
            return result && resultData != null
                       ? resultData.Concat(_endOfLine).ToArray()
                       : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
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

        private byte[] HandleStats(string commandName, string[] commandParams)
        {
            // TODO: implement stats
            return new byte[] {};
        }

        private byte[] HandleDelete(string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            return _cache.Remove(key)
                       ? Encoding.ASCII.GetBytes("DELETED\r\n")
                       : Encoding.ASCII.GetBytes("NOT_FOUND\r\n");
        }

        private byte[] HandleCas(byte[] dataBlock, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var flags = ToFlags(commandParams[1]);
            var exptime = ToExpiry(commandParams[2]);
            var casUnique = ulong.Parse(commandParams[4]);
            return _cache.Cas(key, flags, exptime, casUnique, dataBlock)
                       ? Encoding.ASCII.GetBytes("STORED\r\n")
                       : Encoding.ASCII.GetBytes("EXISTS\r\n");
        }

        private byte[] HandleStore(byte[] dataBlock, string commandName, string[] commandParams)
        {
            var key = ToKey(commandParams[0]);
            var flags = ToFlags(commandParams[1]);
            var exptime = ToExpiry(commandParams[2]);
            return Store(commandName, key, flags, exptime, dataBlock);
        }
    }
}