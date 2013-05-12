using System.Text;

namespace Nemcache.Client.Builders
{
    internal class TouchRequestBuilder : IRequestBuilder
    {
        private readonly string _key;
        private bool _noReply;
        private int _time;

        public TouchRequestBuilder(string key)
        {
            _key = key;
        }

        public byte[] ToAsciiRequest()
        {
            var format = string.Format("touch {0} {1}{2}\r\n",
                                       _key, _time, _noReply ? " noreply" : "");
            return Encoding.ASCII.GetBytes(format);
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
    }
}