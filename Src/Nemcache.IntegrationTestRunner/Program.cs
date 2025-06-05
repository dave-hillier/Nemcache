using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nemcache.Client.Builders;
using Nemcache.Tests.RequestHandlerIntegrationTests;

namespace Nemcache.IntegrationTestRunner
{
    internal class Program
    {
        private static int _count;

        private static void Main(string[] args)
        {
            int port = 11222;
            //int port = 11211;

            var disconnect = new SyncClient(port);
            disconnect.Disconnect();
            

            var client = new SyncClient(port);
            var tests = new object[]
                {
                    new AddTest {Client = client},
                    new AppendTests {Client = client},
                    new DeleteTests {Client = client},
                    new GetAndSetTests {Client = client},
                    new MutateTests {Client = client},
                    new ReplaceTests {Client = client},
                };

            var initializer = from instance in tests
                              let type = instance.GetType()
                              from method in type.GetMethods()
                              where method.GetCustomAttributes(typeof (SetUpAttribute), false).Any()
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
                          where method.GetCustomAttributes(typeof (TestAttribute), false).Any()
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

            var timeSpan = TimeSpan.Zero;
            var lastCount = 0;
            var tf = new TaskFactory();

            stopWatch.Start();

            for (int i = 0; i < 1000; i++)
            {
                tf.StartNew(
                    () =>
                        {
                            var client1 = new SyncClient(port);
                            SimpleBenchmark(client1, string.Format("Key{0}", i), 1000000);
                        });
            }
            tf.StartNew(() =>
                {
                    while (true)
                    {
                        Task.Delay(TimeSpan.FromSeconds(10));
                        if (timeSpan + TimeSpan.FromSeconds(10) < stopWatch.Elapsed)
                        {
                            timeSpan = stopWatch.Elapsed;
                            Console.WriteLine("{0} per second", _count - lastCount);
                            lastCount = _count;
                        }
                    }
                });
//            stopWatch.Stop();
            //Console.WriteLine("Took {0}", stopWatch.Elapsed);

            //Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private static async void SimpleBenchmark(SyncClient client, string key = "key", int iterations = 100000)
        {
            for (int i = 0; i < iterations; ++i)
            {
                var b = new StoreRequestBuilder("set", key, string.Format("Data{0}", i));
                var request = b.ToAsciiRequest();
                var resposne = await client.SendAsync(request);
                Interlocked.Increment(ref _count);
            }

            var get = new GetRequestBuilder("get", key);
            var request2 = get.ToAsciiRequest();
            var resposne2 = client.Send(request2);
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