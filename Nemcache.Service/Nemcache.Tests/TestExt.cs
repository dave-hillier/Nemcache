using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Tests
{
    public static class TestExt
    {
        public static string ToAsciiString(this byte[] s)
        {
            return Encoding.ASCII.GetString(s);
        }
    }

}
