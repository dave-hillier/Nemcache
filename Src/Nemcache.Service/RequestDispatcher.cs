using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nemcache.Storage;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using Nemcache.Service.RequestHandlers;

namespace Nemcache.Service
{
    internal interface IRequestDispatcher
    {
        Task Dispatch(
            Stream stream,
            Stream outStream,
            string remoteEndpoint,
            Action clientDisconnectCallback);
    }

    internal class RequestDispatcher : IRequestDispatcher
    {
        private const int RequestSizeLimit = 256;
        private readonly byte[] _endOfLine = new byte[] {13, 10}; // Ascii for "\r\n"
        private readonly Dictionary<string, IRequestHandler> _requestHandlers;

        public RequestDispatcher(IScheduler scheduler, IMemCache cache, Dictionary<string, IRequestHandler> requestHandlers)
        {
            _requestHandlers = requestHandlers;
        }

        public async Task Dispatch(
            Stream stream,
            Stream outStream,
            string remoteEndpoint,
            Action clientDisconnectCallback)
        {
            try
            {
                var requestContext = await CreateRequestContext(stream, outStream, clientDisconnectCallback);

                if (requestContext == null) // disconnected
                {
                    if (clientDisconnectCallback != null)
                        clientDisconnectCallback();
                    return;
                }

                if (_requestHandlers.ContainsKey(requestContext.CommandName))
                {
                    _requestHandlers[requestContext.CommandName].HandleRequest(requestContext);
                }
                else
                {
                    WriteUnknownCommandResponse(requestContext);
                }
            }
            catch (IOException)
            {
                throw;
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
            var response = Encoding.ASCII.GetBytes(string.Format("ERROR Unknown command: {0}\r\n", requestContext.CommandName));
            requestContext.ResponseStream.WriteAsync(response, 0, response.Length);
        }

        private async Task<RequestContext> CreateRequestContext(Stream stream, Stream outStream,
                                                                Action clientDisconnectCallback)
        {
            var requestFirstLine = GetFirstLine(stream);
            if (string.IsNullOrEmpty(requestFirstLine))
                return null;

            var requestTokens = requestFirstLine.Split(' ');
            var commandName = requestTokens.First();
            var commandParams = requestTokens.Skip(1).ToArray();

            byte[] dataBlock = await GetDataBlock(commandName, stream, commandParams, clientDisconnectCallback);

            var requestContext = new RequestContext(commandName, commandParams,
                                                    dataBlock,
                                                    IsNoReply(commandParams, commandName)
                                                        ? new MemoryStream()
                                                        : outStream,
                                                    clientDisconnectCallback);
            return requestContext;
        }

        private string GetFirstLine(Stream stream)
        {
            var firstLine = GetFirstLineBytes(stream);
            if (firstLine == null)
                return null;

            var requestFirstLine = Encoding.ASCII.GetString(firstLine.ToArray()).TrimEnd();
            return requestFirstLine;
        }

        private static bool IsNoReply(IEnumerable<string> commandParams, string commandName)
        {
            return commandParams.LastOrDefault() == "noreply" && !commandName.StartsWith("get");
        }

        private static async Task<byte[]> GetDataBlock(string commandName, 
            Stream stream, string[] commandParams, Action clientDisconnectCallback)
        {
            byte[] dataBlock = null;
            bool hasDataBlock = IsSetCommand(commandName);
            if (hasDataBlock)
            {
                var bytes = commandParams[3];
                dataBlock = new byte[Int32.Parse(bytes)];

                int read = 0;
                while (read < dataBlock.Length)
                {
                    int count = await stream.ReadAsync(dataBlock, read, dataBlock.Length - read);
                    if (count == 0)
                    {
                        clientDisconnectCallback();
                        return new byte[]{};
                    }
                    read += count;
                }
                // TODO: add a test the stream position
                stream.ReadByte();// \r
                stream.ReadByte();// \n
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

                // Disconnected
                if (current == 0xFF)
                    return null;

                buffer.Add(current);
                if (buffer.Count > RequestSizeLimit)
                    throw new Exception("New line not found"); // TODO: or request too long?
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