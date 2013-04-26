using System.Collections.Generic;
using System.Text;

namespace Nemcache.Client.Builders
{
    internal class GetRequestBuilder : IRequestBuilder
    {
        private readonly string _command;
        private readonly IEnumerable<string> _keys;

        public GetRequestBuilder(string command, params string[] keys)
        {
            _command = command;
            _keys = keys;
        }

        public byte[] ToAsciiRequest()
        {
            return Encoding.ASCII.GetBytes(_command + " " + string.Join(" ", _keys) + "\r\n");
        }
    }
}