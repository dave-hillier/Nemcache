using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Nemcache.Service.Commands;

namespace Nemcache.Service
{
    internal class Program 
    {
        private readonly Dictionary<string, ICommand> _commands;

        private static void Main(string[] args)
        {
            var p = new Program();
            var server = new RequestResponseTcpServer(IPAddress.Any, 11222, p.Dispatch);
            Console.ReadLine();
        }

        public Program()
        {
            var cache = new ArrayMemoryCache();
            var get = new GetCommand(cache);
            var set = new SetCommand(cache);
            var delete = new DeleteCommand(cache);
            _commands = new Dictionary<string, ICommand>
                {
                    { get.Name, get }, 
                    { set.Name, set },
                    { delete.Name, delete },
                };
        }

        public byte[] Dispatch(string remoteEndpoint, byte[] data)
        {
            Console.WriteLine("In: {0}", Encoding.ASCII.GetString(data));
            var request = new AsciiRequest(data);

            var command = _commands[request.CommandName];
            var result = command.Execute(request);
            Console.WriteLine("Out: {0}", Encoding.ASCII.GetString(result));
            return result;
        }
    }
}