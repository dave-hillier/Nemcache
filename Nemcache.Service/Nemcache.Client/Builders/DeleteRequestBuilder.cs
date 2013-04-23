using System.Linq;
using System.Text;

namespace Nemcache.Client.Builders
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

        public byte[] ToAsciiRequest()
        {
            var format = string.Format("delete {0}{1}\r\n", _key, _noReply ? " noreply" : "");
            return Encoding.ASCII.GetBytes(format + "\r\n");
        }
    }
}