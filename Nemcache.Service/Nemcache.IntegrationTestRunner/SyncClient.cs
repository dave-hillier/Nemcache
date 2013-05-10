using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Nemcache.Tests.RequestHandlerIntegrationTests;

namespace Nemcache.IntegrationTestRunner
{
    internal class SyncClient : IClient
    {
        private readonly NetworkStream _stream;
        private readonly TcpClient _tcpClient = new TcpClient();

        public SyncClient(int port)
        {
            _tcpClient.Connect("127.0.0.1", port);
            _stream = _tcpClient.GetStream();
        }

        public void Disconnect()
        {
            _tcpClient.Close();
            
        }

        public byte[] Send(byte[] request)
        {
            var memoryStream = new MemoryStream(request);

            var requestString = Encoding.ASCII.GetString(request);
            //Console.WriteLine("Requesting: {0}", requestString);
            memoryStream.CopyTo(_stream);

            if (requestString.Contains("noreply"))
            {
                return new byte[] {};
            }
            var buffer = new byte[4096];
            int read = _stream.Read(buffer, 0, 4096);
            var result = buffer.Take(read).ToArray();
            //Console.WriteLine("  Response: {0}", Encoding.ASCII.GetString(result));
            return result;
        }


        public IDisposable OnDisconnect { get; set; }

        public async Task<byte[]> SendAsync(byte[] request)
        {
            //var memoryStream = new MemoryStream(request);

            var requestString = Encoding.ASCII.GetString(request);
            //Console.WriteLine("Requesting: {0}", requestString);
            await _stream.WriteAsync(request, 0, request.Length);

            if (requestString.Contains("noreply"))
            {
                return new byte[] {};
            }
            var buffer = new byte[4096];
            int read = await _stream.ReadAsync(buffer, 0, 4096);
            var result = buffer.Take(read).ToArray();

            //Console.WriteLine("  Response: {0}", Encoding.ASCII.GetString(result));

            return result;
        }
    }
}