using System.Collections.Generic;
using System.Text;

namespace Nemcache.Tests
{
    class MemcacheGetCommandBuilder
    {
        private readonly string _command;
        private readonly IEnumerable<string> _keys;

        public MemcacheGetCommandBuilder(string command, params string[] keys)
        {
            _command = command;
            _keys = keys;
        }

        public byte[] ToRequest()
        {
            return Encoding.ASCII.GetBytes(_command + " " + string.Join(",", _keys) + "\r\n");
        }
    }
}