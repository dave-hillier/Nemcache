using System.Linq;
using System.Text;

namespace Nemcache.Tests.Builders
{
    class MemcacheDeleteCommandBuilder
    {
        private readonly string _key;
        private bool _noReply;

        public MemcacheDeleteCommandBuilder(string key)
        {
            _key = key;
        }

        public MemcacheDeleteCommandBuilder NoReply()
        {
            _noReply = true;
            return this;
        }

        public byte[] ToRequest()
        {
            var format = string.Format("Delete {0}{1}\r\n", _key, _noReply ? " noreply" : "");
            var start = Encoding.ASCII.GetBytes(format);
            var end = Encoding.ASCII.GetBytes("\r\n");
            return start.Concat(end).ToArray();
        }
    }
}