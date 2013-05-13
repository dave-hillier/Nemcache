using System.Collections.Generic;
using System.IO;

namespace Nemcache.Service
{
    public interface IRequestContext
    {
        string CommandName { get; }
        IEnumerable<string> Parameters { get; }
        byte[] DataBlock { get; }

        Stream ResponseStream { get; }
        void Close();
    }
}