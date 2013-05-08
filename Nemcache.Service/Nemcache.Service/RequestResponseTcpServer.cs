using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class RequestContext : IRequestContext
    {
        private readonly Action _disconnectCallback;

        public RequestContext(string name, 
            IEnumerable<string> args, 
            byte[] dataBlock, 
            Stream responseStream, Action disconnectCallback)
        {
            _disconnectCallback = disconnectCallback;
            CommandName = name;
            Parameters = args;
            DataBlock = dataBlock;
            ResponseStream = responseStream;

        }

        public string CommandName { get; private set; }
        public IEnumerable<string> Parameters { get; private set; }
        public byte[] DataBlock { get; private set; }
        public void Close()
        {
            _disconnectCallback();
        }

        public Stream ResponseStream { get; private set; }
    }

    class RequestResponseTcpServer
    {
        private readonly Func<Stream, Stream, string, IDisposable, Task> _callback;
        private readonly TaskFactory _taskFactory;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            
        

        public IPAddress Address { get; set; }
        public int Port { get; set; }
        private readonly TcpListener _listener;

        public RequestResponseTcpServer(IPAddress address, int port,
                              Func<Stream, Stream, string, IDisposable, Task> callback)
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
                        
                        await _callback(stream, stream, "", Disposable.Create(() =>
                            {
                                disconnected = true;
                                tcpClient.Close();
                            }));
                        //await stream.WriteAsync(responseStream, 0, responseStream.Length);
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