using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    internal class Program 
    {
        private readonly Dictionary<string, ICommand> _commands;

        private static void Main(string[] args)
        {
            var p = new Program();
            var server = new BasicTcpServer(IPAddress.Any, 11222, p.Dispatch);
            Console.ReadLine();
        }

        public Program()
        {
            var cache = new ArrayMemoryCache();
            var get = new GetCommand(cache);
            var set = new SetCommand(cache);
            _commands = new Dictionary<string, ICommand> {{get.Name, get}, {set.Name, set}};
        }

        public void Dispatch(string remoteEndpoint, byte[] data, NetworkStream networkStream)
        {
            var request = new AsciiRequest(data);

            var command = _commands[request.CommandName];
            var response = command.Execute(request);

            networkStream.Write(response, 0, response.Length);
        }
    }

    internal class BasicTcpServer
    {
        private readonly Action<string, byte[], NetworkStream> _callback;
        private readonly TcpListener _tcpListener;
        private readonly CancellationTokenSource _tokenSource;
        private readonly TaskFactory _taskFactory;

        public BasicTcpServer(IPAddress address, int port, Action<string, byte[], NetworkStream> callback)
        {
            _callback = callback;
            _tokenSource = new CancellationTokenSource();
            _taskFactory = new TaskFactory(_tokenSource.Token);

            _tcpListener = new TcpListener(address, port);

            _tcpListener.Start();
            AcceptClient(_tcpListener);
        }

        private void AcceptClient(TcpListener tcpListener)
        {
            var acceptTcpClientTask = _taskFactory.FromAsync<TcpClient>(
                tcpListener.BeginAcceptTcpClient,
                tcpListener.EndAcceptTcpClient,
                tcpListener);

            acceptTcpClientTask.
                ContinueWith(task => OnAcceptConnection(task.Result), TaskContinuationOptions.OnlyOnRanToCompletion).
                ContinueWith(task => AcceptClient(tcpListener), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private void OnAcceptConnection(TcpClient tcpClient)
        {
            string remoteEndpoint = tcpClient.Client.RemoteEndPoint.ToString();
            var networkStream = tcpClient.GetStream();

            // TODO: create a reader that will make the buffer bigger
            var bytes = new byte[4096];
            int i = 0;
            while ((i = networkStream.Read(bytes, 0, bytes.Length)) != 0)
            {
                // Translate data bytes to a ASCII string.
                var text = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                Console.WriteLine("Received: {0}", text);
                _callback(remoteEndpoint, bytes, networkStream);
            }
        }
    }
}