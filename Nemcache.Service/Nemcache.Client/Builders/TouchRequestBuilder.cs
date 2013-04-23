using System.Linq;
using System.Text;

namespace Nemcache.Client.Builders
{
    class TouchRequestBuilder : IRequestBuilder
    {
        private readonly string _key;
        private int _time;
        private bool _noReply;

        public TouchRequestBuilder(string key)
        {
            _key = key;
        }

        public TouchRequestBuilder WithExpiry(int time)
        {
            _time = time;
            return this;
        }

        public TouchRequestBuilder NoReply()
        {
            _noReply = true;
            return this;
        }

        public byte[] ToAsciiRequest()
        {
            var format = string.Format("touch {0} {1}{2}\r\n",
                                       _key, _time, _noReply ? " noreply" : "");
            var start = Encoding.ASCII.GetBytes(format);
            var end = Encoding.ASCII.GetBytes("\r\n");
            return start.Concat(end).ToArray();
        }
    }
}