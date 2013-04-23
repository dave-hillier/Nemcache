using System;
using System.Linq;
using System.Text;

namespace Nemcache.Client.Builders
{
    class CasRequestBuilder : StoreRequestBuilder
    {
        private ulong _casUnique;

        public CasRequestBuilder(string key, byte[] data) : 
            base("cas", key, data)
        {
        }

        public CasRequestBuilder(string key, string data) :
            base("cas", key, Encoding.ASCII.GetBytes(data))
        {
        }

        public CasRequestBuilder WithCasUnique(ulong casUnique)
        {
            _casUnique = casUnique;
            return this;
        }

        public override byte[] ToAsciiRequest()
        {
            var format = string.Format("cas {0} {1} {2} {3} {4}{5}\r\n",
                                        _key, _flags, _time, _data.Length, _casUnique, _noReply ? " noreply" : "");
            return Encoding.ASCII.GetBytes(format).
                Concat(_data).
                Concat(Encoding.ASCII.GetBytes("\r\n")).
                ToArray();
        }
    }
}