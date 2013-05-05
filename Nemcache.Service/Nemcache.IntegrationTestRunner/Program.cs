using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Client.Builders;
using Nemcache.Tests.RequestHandlerIntegrationTests;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Nemcache.IntegrationTestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 11222;
            //int port = 11211;

            var client = new SyncClient(port);
            var tests = new object[]
                {
                    new AddTest { Client = client },
                    new AppendTests { Client = client },
                    new DeleteTests { Client = client },
                    new GetAndSetTests { Client = client },
                    new MutateTests { Client = client },
                    new ReplaceTests { Client = client },
                };

            var initializer = from instance in tests
                          let type = instance.GetType()
                          from method in type.GetMethods()
                          where method.GetCustomAttributes(typeof(TestInitializeAttribute), false).Any()
                          select new Action(() =>
                              {
                                  try
                                  {
                                      method.Invoke(instance, null);
                                  }
                                  catch (Exception)
                                  {
                                      Console.WriteLine("[ERROR] {0} {1}...", type.Name, method.Name);                                      
                                  }
                              });

            foreach (var action in initializer)
            {
                action();
            }

            var actions = from instance in tests
                          let type = instance.GetType()
                          from method in type.GetMethods()
                          where method.GetCustomAttributes(typeof (TestMethodAttribute), false).Any()
                          select new Action(() =>
                          {
                              try
                              {
                                  method.Invoke(instance, null);
                              }
                              catch (Exception ex)
                              {
                                  Console.WriteLine("[ERROR] {0} {1} {2}...", type.Name, method.Name, ex.Message);
                              }
                          });
            client.Send(Encoding.ASCII.GetBytes("flush_all\r\n"));
            foreach (var action in actions)
            {
                action();
                client.Send(Encoding.ASCII.GetBytes("flush_all\r\n"));
            }

            Console.WriteLine("Tests complete!");

            client.Send(Encoding.ASCII.GetBytes("flush_all\r\n"));


            var stopWatch = new Stopwatch();
            stopWatch.Start();
            Parallel.For(0, 1, i =>
                {
                    var client1 = new SyncClient(port);
                    SimpleBenchmark(client1, string.Format("Key{0}", i), 100000);
                });
            stopWatch.Stop();
            Console.WriteLine("Took {0}", stopWatch.Elapsed);
 
            Console.WriteLine("Done!");
            Console.ReadLine();

        }

        private static void SimpleBenchmark(SyncClient client, string key = "key", int iterations = 100000)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int i = 0; i < iterations; ++i)
            {
                var b = new StoreRequestBuilder("set", key, string.Format("Data{0}", i));
                var request = b.ToAsciiRequest();
                //Console.WriteLine("Send!");
                var resposne = client.Send(request);
                //Console.WriteLine("{0}", Encoding.ASCII.GetString(resposne));
            }

            var get = new GetRequestBuilder("get", key);
            var request2 = get.ToAsciiRequest();
            var resposne2 = client.Send(request2);
            stopWatch.Stop();
            //Console.WriteLine("{0} after {1}", Encoding.ASCII.GetString(resposne2), stopWatch.Elapsed);
        }

        private static void Connected(TcpClient client)
        {
            var s = client.GetStream();
            SendRequest(s).ContinueWith(_ => Connected(client));
        }

        private static Task SendRequest(NetworkStream s)
        {
            var b = new StoreRequestBuilder("set", "mykey", "MyData");
            var request = b.ToAsciiRequest();
            return s.WriteAsync(request, 0, request.Length);
        }
    }
}
