using System.Linq;
using System.Text;

namespace Nemcache.Tests
{
    class MemcacheCommandBuilder
    {
        private readonly string _command;
        private readonly string _key;
        private readonly byte[] _data;
        private short _flags;
        private int _time;
        private bool _noReply;

        public MemcacheCommandBuilder(string command, string key, byte[] data)
        {
            _command = command;
            _key = key;
            _data = data;
        }

        public MemcacheCommandBuilder WithFlags(short flag)
        {
            _flags = flag;
            return this;
        }

        public MemcacheCommandBuilder WithExpiry(int time)
        {
            _time = time;
            return this;
        }

        public MemcacheCommandBuilder NoReply()
        {
            _noReply = true;
            return this;
        }

        public byte[] ToRequest()
        {
            var format = string.Format("{0} {1} {2} {3} {4}{5}\r\n",
                                       _command, _key, _flags, _time, _data.Length, _noReply ? " noreply" : "");
            var start = Encoding.ASCII.GetBytes(format);
            var end = Encoding.ASCII.GetBytes("\r\n");
            return start.Concat(_data).Concat(end).ToArray();
        }
    }
}