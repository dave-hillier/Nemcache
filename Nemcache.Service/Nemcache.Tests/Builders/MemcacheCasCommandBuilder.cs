using System;
using System.Linq;
using System.Text;

namespace Nemcache.Tests.Builders
{
    class MemcacheCasCommandBuilder : MemcacheStorageCommandBuilder
    {
        private ulong _casUnique;

        public MemcacheCasCommandBuilder(string key, byte[] data) : 
            base("cas", key, data)
        {
        }

        public MemcacheCasCommandBuilder(string key, string data) :
            base("cas", key, Encoding.ASCII.GetBytes(data))
        {
        }

        public MemcacheCasCommandBuilder WithCasUnique(ulong casUnique)
        {
            _casUnique = casUnique;
            return this;
        }

        public override byte[] ToRequest()
        {
            /*var format = string.Format("cas {0} {1} {2} {3} {4}{5}\r\n",
                                        _key, _flags, _time, _data.Length, _casUnique, _noReply ? " noreply" : "");
            var start = Encoding.ASCII.GetBytes(format);
            var end = Encoding.ASCII.GetBytes("\r\n");
            return start.Concat(_data).Concat(end).ToArray();*/

            throw new NotImplementedException();
        }
    }
}