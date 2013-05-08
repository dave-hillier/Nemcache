using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    internal class RequestResponseTcpServer
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly RequestDispatcher _dispatcher;
        private readonly TcpListener _listener;
        private readonly TaskFactory _taskFactory;

        public RequestResponseTcpServer(IPAddress address, int port, RequestDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _taskFactory = new TaskFactory(_cancellationTokenSource.Token);
            Address = address;
            Port = port;
            _listener = new TcpListener(address, port);
        }

        public IPAddress Address { get; set; }
        public int Port { get; set; }

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
                        await _dispatcher.Dispatch(stream, stream, "", Disposable.Create(() =>
                            {
                                disconnected = true;
                                tcpClient.Close();
                            }));
                    }
                }
            }
            catch (IOException exception)
            {
                Console.WriteLine("[ERROR] {0}", exception.Message);
            }
        }

        public void Stop()
        {
            _listener.Stop();
            _cancellationTokenSource.Cancel();
        }
    }
}