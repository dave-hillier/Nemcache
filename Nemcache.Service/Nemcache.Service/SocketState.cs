using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class SocketState
    {
        private readonly CancellationTokenSource _tokenSource;
        private readonly TaskFactory _taskFactory;
        private readonly Socket _socket;
        private readonly NetworkStream _stream;

        public SocketState(Socket socket)
        {
            _tokenSource = new CancellationTokenSource();
            _taskFactory = new TaskFactory(_tokenSource.Token);
            _socket = socket;
            if (_socket.Connected)
                _stream = new NetworkStream(_socket);            
        }

        public Socket Socket { get { return _socket; } }

        public Task<SocketState> Accept()
        {
            var task = _taskFactory.StartNew(() =>
                {
                    Console.WriteLine("Accept");
                    var clientSocket = _socket.Accept(); // TODO: to async
                    return new SocketState(clientSocket);
                });
            return task;
        }

        public NetworkStream Stream { get { return _stream; } }
    }
}