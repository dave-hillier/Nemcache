using System.Text;

namespace Nemcache.Tests.Builders
{
    class MemcacheIncrCommandBuilder
    {
        private readonly string _command;
        private readonly ulong _value;
        private bool _noReply;

        public MemcacheIncrCommandBuilder(string command, ulong value)
        {
            _command = command;
            _value = value;
        }

        public MemcacheIncrCommandBuilder NoReply()
        {
            _noReply = true;
            return this;
        }

        public byte[] ToRequest()
        {
            var result = _command + " " + _value;
            if (_noReply)
                result += " " + _noReply;
            return Encoding.ASCII.GetBytes(result + "\r\n");
        }
    }
}