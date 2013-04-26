using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    // TODO: Some tests?
    internal class RequestResponseTcpServer
    {
        private const int BufferSize = 4096;
        private readonly Func<string, byte[], IDisposable, byte[]> _callback;
        private readonly TaskFactory _taskFactory;
        private readonly TcpListener _tcpListener;
        private readonly CancellationTokenSource _tokenSource;

        // TODO: use an interface for the callback?
        public RequestResponseTcpServer(IPAddress address, int port, Func<string, byte[], IDisposable, byte[]> callback)
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
            HandleRequest(remoteEndpoint, networkStream);
        }

        private void HandleRequest(string remoteEndpoint, NetworkStream networkStream)
        {
            var readTask = Read(networkStream, BufferSize).
                ContinueWith(task =>
                    {
                        var memoryStream = task.Result;
                        byte[] input = memoryStream.ToArray();
                        if (input.Length != 0)
                        {
                            var output = _callback(remoteEndpoint, input, Disposable.Create(networkStream.Close));
                            WriteResponse(networkStream, output);
                        }
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);

            readTask.ContinueWith(t => HandleRequest(remoteEndpoint, networkStream),
                                  TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private static void WriteResponse(NetworkStream networkStream, byte[] output)
        {
            var responseStream = new MemoryStream(output);
            responseStream.CopyTo(networkStream);
        }

        private Task<MemoryStream> Read(NetworkStream networkStream, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            var read = networkStream.
                ReadAsync(buffer, 0, bufferSize).
                ToObservable().
                DoWhile(() => networkStream.DataAvailable && networkStream.CanRead).
                TakeWhile(c => c > 0).
                Scan(new MemoryStream(), (stream, readCount) =>
                    {
                        stream.Write(buffer, 0, readCount);
                        return stream;
                    });
            return read.ToTask(_tokenSource.Token);
        }
    }
}