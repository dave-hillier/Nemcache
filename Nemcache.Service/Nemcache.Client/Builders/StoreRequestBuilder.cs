using System.Linq;
using System.Text;

namespace Nemcache.Client.Builders
{
    public class StoreRequestBuilder : IRequestBuilder
    {
        private readonly string _command;

        protected readonly string _key;
        protected byte[] _data;
        protected ulong _flags;
        protected bool _noReply;
        protected int _time;

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

        public virtual byte[] ToAsciiRequest()
        {
            var format = string.Format("{0} {1} {2} {3} {4}{5}\r\n",
                                       _command, _key, _flags, _time, _data.Length, _noReply ? " noreply" : "");
            return Encoding.ASCII.GetBytes(format).
                            Concat(_data).
                            Concat(Encoding.ASCII.GetBytes("\r\n")).
                            ToArray();
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
    }
}