using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Nemcache.Tests.RequestHandlerIntegrationTests;

namespace Nemcache.IntegrationTestRunner
{
    class SyncClient : IClient
    {
        readonly TcpClient _tcpClient = new TcpClient();
        private readonly NetworkStream _stream;

        public SyncClient(int port)
        {
            _tcpClient.Connect("127.0.0.1", port);
            _stream = _tcpClient.GetStream();
        }

        public byte[] Send(byte[] request)
        {
            var memoryStream = new MemoryStream(request); 

            var requestString = Encoding.ASCII.GetString(request);
            //Console.WriteLine("Requesting: {0}", requestString);
            memoryStream.CopyTo(_stream);

            if (requestString.Contains("noreply"))
            {
                return new byte[]{};
            }
            var buffer = new byte[4096];
            int read = _stream.Read(buffer, 0, 4096);
            var result = buffer.Take(read).ToArray();

            //Console.WriteLine("  Response: {0}", Encoding.ASCII.GetString(result));

            return result;

        }

        public IDisposable OnDisconnect { get; set; }
    }
}