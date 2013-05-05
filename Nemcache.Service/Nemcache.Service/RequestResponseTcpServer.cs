using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class RequestResponseTcpServer
    {
        private const int BufferSize = 8112;

        private readonly Func<string, byte[], IDisposable, byte[]> _callback;
        private readonly TaskFactory _taskFactory;

        public IPAddress Address { get; set; }
        public int Port { get; set; }
        private readonly SocketState _socket;

        public RequestResponseTcpServer(IPAddress address, int port,
                              Func<string, byte[], IDisposable, byte[]> callback)
        {
            _taskFactory = new TaskFactory();
            _callback = callback;
            Address = address;
            Port = port;
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _socket = new SocketState(socket);

        }

        public void Start()
        {
            var ipLocal = new IPEndPoint(IPAddress.Any, Port);
            _socket.Socket.Bind(ipLocal);
            _socket.Socket.Listen(100); // TODO: what does the backlog mean? Max connections?
            _taskFactory.StartNew(() => 
                {
                    while (true)
                    {
                        ListenForClients().Wait();
                    }
                });
        }

        private async Task ListenForClients()
        {
            var client = await _socket.Accept();
            OnClientConnection(client);
            await Task.Yield();
        }

        private void OnClientConnection(SocketState clientSocket)
        {
            ReadAndRespond(clientSocket.Stream);
        }

        private async Task ReadAndRespond(NetworkStream networkStream)
        {
            bool clientDisconnect = false;
            while (!clientDisconnect)
            {
                if (networkStream.DataAvailable)
                {
                    var request = await ReadRequest(networkStream);

                    if (request.Length > 0)
                    {
                        //Console.WriteLine("Request: {0}", Encoding.ASCII.GetString(request));
                        var response = _callback("", request, Disposable.Create(networkStream.Close));
                        if (response.Length > 0)
                        {
                            WriteResponse(networkStream, response);
                            //Console.WriteLine("Response: {0}", Encoding.ASCII.GetString(response));
                        }
                    }
                    else
                    {
                        clientDisconnect = true;
                    }
                }
                //await Task.Yield();
            }
        }

        private static async Task<byte[]> ReadRequest(NetworkStream networkStream)
        {
            var memoryStream = new MemoryStream();
            var buffer = new byte[BufferSize];
            int count;
            while (networkStream.CanRead &&
                   networkStream.DataAvailable &&
                   (count = await networkStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await memoryStream.WriteAsync(buffer, 0, count);
            }
            return memoryStream.ToArray();
        }

        private static void WriteResponse(NetworkStream networkStream, byte[] output)
        {
            var responseStream = new MemoryStream(output);
            responseStream.CopyTo(networkStream);
        }

        public void Stop()
        {
        }
    }
}