using System.Collections.Generic;
using System.IO;

namespace Nemcache.Service
{
    internal interface IRequestContext
    {
        string CommandName { get; }
        IEnumerable<string> Parameters { get; }
        byte[] DataBlock { get; }

        void Close();
        Stream ResponseStream { get; }
    }
}