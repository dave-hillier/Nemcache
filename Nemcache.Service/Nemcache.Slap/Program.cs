using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Nemcache.Client.Builders;

namespace Nemcache.Slap
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Nemcache.Slap Simple Load Test");
            int port = 11222;

            var tcpClient = new TcpClient();
            
            var connectTask = tcpClient.ConnectAsync("127.0.0.1", port);

            connectTask.
                ContinueWith(c => Connected(tcpClient), TaskContinuationOptions.OnlyOnRanToCompletion).
                ContinueWith(_ => Console.WriteLine("Error"), TaskContinuationOptions.OnlyOnFaulted);
            
            Console.ReadLine();
        }

        private static void Connected(TcpClient client)
        {
            var s = client.GetStream();
            SendRequest(s).ContinueWith(_ =>
                {
                    Console.WriteLine("Sent");
                    Connected(client);
                });
        }

        private static Task SendRequest(NetworkStream s)
        {
            var b = new StoreRequestBuilder("set", "mykey", "MyData");
            var request = b.ToAsciiRequest();
            return s.WriteAsync(request, 0, request.Length);
        }
    }
}
