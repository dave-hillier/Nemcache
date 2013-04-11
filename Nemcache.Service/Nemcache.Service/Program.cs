using System;
using System.Collections.Generic;
using System.Linq;
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

            // TODO: reflect the assembly to get these?
            _commands = (new ICommand[]
                {
                    new GetCommand(cache),
                    new SetCommand(cache),
                    new IncrCommand(cache),
                    new DecrCommand(cache),
                    new DeleteCommand(cache),
                }).ToDictionary(c => c.Name, c => c);
        }

        public byte[] Dispatch(string remoteEndpoint, byte[] data)
        {
            try
            {
                var request = new AsciiRequest(data);
                if (_commands.ContainsKey(request.CommandName))
                {
                    var command = _commands[request.CommandName];
                    return command.Execute(request);
                }
                return Encoding.ASCII.GetBytes(string.Format("CLIENT_ERROR Unknown command {0}\r\n", request.CommandName));
            }
            catch (Exception ex)
            {
                return Encoding.ASCII.GetBytes(string.Format("SERVER_ERROR {0}\r\n", ex.Message));
            }

        }
    }
}