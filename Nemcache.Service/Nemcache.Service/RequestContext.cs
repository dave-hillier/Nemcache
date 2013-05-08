using System;
using System.Collections.Generic;
using System.IO;

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
}