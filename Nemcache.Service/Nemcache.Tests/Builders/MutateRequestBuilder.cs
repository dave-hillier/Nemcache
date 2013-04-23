using System.Text;

namespace Nemcache.Tests.Builders
{
    class MutateRequestBuilder
    {
        private readonly string _command;
        private readonly string _key;
        private readonly ulong _value;
        private bool _noReply;

        public MutateRequestBuilder(string command, string key, ulong value)
        {
            _command = command;
            _key = key;
            _value = value;
        }

        public MutateRequestBuilder NoReply()
        {
            _noReply = true;
            return this;
        }

        public byte[] ToRequest()
        {
            var result = string.Format("{0} {1} {2}", _command, _key, _value);
            if (_noReply)
                result += " " + _noReply;
            return Encoding.ASCII.GetBytes(result + "\r\n");
        }
    }
}