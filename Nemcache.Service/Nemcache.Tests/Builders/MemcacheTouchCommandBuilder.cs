using System.Linq;
using System.Text;

namespace Nemcache.Tests.Builders
{
    class MemcacheTouchCommandBuilder
    {
        private readonly string _key;
        private int _time;
        private bool _noReply;

        public MemcacheTouchCommandBuilder(string key)
        {
            _key = key;
        }

        public MemcacheTouchCommandBuilder WithExpiry(int time)
        {
            _time = time;
            return this;
        }

        public MemcacheTouchCommandBuilder NoReply()
        {
            _noReply = true;
            return this;
        }

        public byte[] ToRequest()
        {
            var format = string.Format("touch {0} {1}{2}\r\n",
                                       _key, _time, _noReply ? " noreply" : "");
            var start = Encoding.ASCII.GetBytes(format);
            var end = Encoding.ASCII.GetBytes("\r\n");
            return start.Concat(end).ToArray();
        }
    }
}