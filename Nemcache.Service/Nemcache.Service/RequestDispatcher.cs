using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using Nemcache.Service.RequestHandlers;

namespace Nemcache.Service
{
    internal class RequestDispatcher
    {
        private const int RequestSizeLimit = 1024;
        private readonly byte[] _endOfLine = new byte[] {13, 10}; // Ascii for "\r\n"
        private readonly Dictionary<string, IRequestHandler> _requestHandlers;

        public RequestDispatcher(IScheduler scheduler, IMemCache cache)
        {
            var helpers = new RequestConverters(scheduler);
            var getHandler = new GetHandler(helpers, cache, scheduler);
            var mutateHandler = new MutateHandler(helpers, cache, scheduler);
            _requestHandlers = new Dictionary<string, IRequestHandler>
                {
                    {"get", getHandler},
                    {"gets", getHandler},
                    {"set", new SetHandler(helpers, cache)},
                    {"append", new AppendHandler(helpers, cache, scheduler)},
                    {"prepend", new PrependHandler(helpers, cache, scheduler)},
                    {"add", new AddHandler(helpers, cache, scheduler)},
                    {"replace", new ReplaceHandler(helpers, cache, scheduler)},
                    {"cas", new CasHandler(helpers, cache)},
                    {"stats", new StatsHandler()},
                    {"delete", new DeleteHandler(helpers, cache)},
                    {"flush_all", new FlushHandler(helpers, cache, scheduler)},
                    {"quit", new QuitHandler()},
                    {"exception", new ExceptionHandler()},
                    {"version", new VersionHandler()},
                    {"touch", new TouchHandler(helpers, cache)},
                    {"incr", mutateHandler},
                    {"decr", mutateHandler},
                };
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

        public async Task Dispatch(
            Stream stream, 
            Stream outStream, 
            string remoteEndpoint,
            IDisposable clientConnectionHandle)
        {
            try
            {
                var requestContext = await CreateRequestContext(stream, outStream, clientConnectionHandle);

                if (_requestHandlers.ContainsKey(requestContext.CommandName))
                {
                    _requestHandlers[requestContext.CommandName].HandleRequest(requestContext);
                }
                else
                {
                    WriteUnknownCommandResponse(requestContext);
                }
            }
            catch (Exception ex)
            {
                WriteExceptionResponse(outStream, ex);
            }
        }

        private static void WriteExceptionResponse(Stream outStream, Exception ex)
        {
            var errorResponse = Encoding.ASCII.GetBytes(string.Format("SERVER ERROR {0}\r\n", ex.Message));
            outStream.WriteAsync(errorResponse, 0, errorResponse.Length);
        }

        private static void WriteUnknownCommandResponse(RequestContext requestContext)
        {
            var response = Encoding.ASCII.GetBytes("ERROR\r\n");
            requestContext.ResponseStream.WriteAsync(response, 0, response.Length);
        }

        private async Task<RequestContext> CreateRequestContext(Stream stream, Stream outStream, IDisposable clientConnectionHandle)
        {
            var requestFirstLine = GetFirstLine(stream);

            var requestTokens = requestFirstLine.Split(' ');
            var commandName = requestTokens.First();
            var commandParams = requestTokens.Skip(1).ToArray();

            byte[] dataBlock = await GetDataBlock(commandName, stream, commandParams);

            var requestContext = new RequestContext(commandName, commandParams,
                                                    dataBlock,
                                                    IsNoReply(commandParams, commandName)
                                                        ? new MemoryStream()
                                                        : outStream,
                                                    () => clientConnectionHandle.Dispose());
            return requestContext;
        }

        private string GetFirstLine(Stream stream)
        {
            var firstLine = GetFirstLineBytes(stream);

            var requestFirstLine = Encoding.ASCII.GetString(firstLine.ToArray()).TrimEnd();
            return requestFirstLine;
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
                int count = await stream.ReadAsync(dataBlock, 0, dataBlock.Length);
                // TODO: does this need to repeat for large payloads?
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

        private List<byte> GetFirstLineBytes(Stream stream)
        {
            var buffer = new List<byte>();
            byte last = 0;
            while (true)
            {
                var current = (byte) stream.ReadByte();

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
    }
}