using System.Linq;
using System.Text;

namespace Nemcache.Client.Builders
{


    class StoreRequestBuilder : IRequestBuilder
    {
        private readonly string _command;

        protected readonly string _key;
        protected byte[] _data;
        protected ulong _flags;
        protected int _time;
        protected bool _noReply;

        public StoreRequestBuilder(string command, string key, byte[] data)
        {
            _command = command;
            _key = key;
            _data = data;
        }

        public StoreRequestBuilder(string command, string key, string data) :
            this(command, key, Encoding.ASCII.GetBytes(data))
        {
        }

        public StoreRequestBuilder WithFlags(ulong flag)
        {
            _flags = flag;
            return this;
        }

        public StoreRequestBuilder WithExpiry(int time)
        {
            _time = time;
            return this;
        }

        public StoreRequestBuilder NoReply()
        {
            _noReply = true;
            return this;
        }

        public StoreRequestBuilder Data(string value)
        {
            _data = Encoding.ASCII.GetBytes(value);
            return this;
        }

        public virtual byte[] ToAsciiRequest()
        {
            var format = string.Format("{0} {1} {2} {3} {4}{5}\r\n",
                                       _command, _key, _flags, _time, _data.Length, _noReply ? " noreply" : "");
            var start = Encoding.ASCII.GetBytes(format);
            var end = Encoding.ASCII.GetBytes("\r\n");
            return start.Concat(_data).Concat(end).ToArray();
        }
    }
}