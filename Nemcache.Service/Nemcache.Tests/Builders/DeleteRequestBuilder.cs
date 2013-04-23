using System.Linq;
using System.Text;

namespace Nemcache.Tests.Builders
{
    class DeleteRequestBuilder
    {
        private readonly string _key;
        private bool _noReply;

        public DeleteRequestBuilder(string key)
        {
            _key = key;
        }

        public DeleteRequestBuilder NoReply()
        {
            _noReply = true;
            return this;
        }

        public byte[] ToRequest()
        {
            var format = string.Format("delete {0}{1}\r\n", _key, _noReply ? " noreply" : "");
            var start = Encoding.ASCII.GetBytes(format);
            var end = Encoding.ASCII.GetBytes("\r\n");
            return start.Concat(end).ToArray();
        }
    }
}