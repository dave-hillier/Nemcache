using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Nemcache.Service
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var requestHandler = new RequestHandler();
            var server = new RequestResponseTcpServer(IPAddress.Any, 11222, requestHandler.Dispatch);
            Console.ReadLine();
        }
    }
}