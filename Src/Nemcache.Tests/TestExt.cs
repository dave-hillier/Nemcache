using System.Text;

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