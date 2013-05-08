using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class RequestResponseTcpServer
    {
        private readonly Func<Stream, string, IDisposable, Task<byte[]>> _callback;
        private readonly TaskFactory _taskFactory;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            
        

        public IPAddress Address { get; set; }
        public int Port { get; set; }
        private readonly TcpListener _listener;

        public RequestResponseTcpServer(IPAddress address, int port,
                              Func<Stream, string, IDisposable, Task<byte[]>> callback)
        {
            _taskFactory = new TaskFactory(_cancellationTokenSource.Token);
            _callback = callback;
            Address = address;
            Port = port;
            _listener = new TcpListener(address, port);
        }

        public void Start()
        {
            _listener.Start();

            _taskFactory.StartNew(ListenForClients);
        }

        private async void ListenForClients()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync();
                tcpClient.NoDelay = true;

                await _taskFactory.StartNew(() => OnClientConnection(tcpClient));
            }
        }

        private async void OnClientConnection(TcpClient tcpClient)
        {
            bool disconnected = false;
            try
            {
                using (var stream = tcpClient.GetStream())
                {
                    while (!disconnected)
                    {
                        byte[] response = await _callback(stream, "", Disposable.Create(() =>
                            {
                                disconnected = true;
                                tcpClient.Close();
                            }));
                        await stream.WriteAsync(response, 0, response.Length);
                    }
                }
            }
            catch(IOException exception)
            {
                Console.WriteLine("[ERROR] {0}", exception.Message);
            }
        }

        public void Stop()
        {
        }
    }
}