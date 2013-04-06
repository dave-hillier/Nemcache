using System;
using System.Linq;
using System.Text;

namespace Nemcache.Service
{
    class AsciiRequest : IRequest
    {
        public AsciiRequest(byte[] input)
        {
            int firstLineLength = 0;
            for (int i = 0; i < input.Length-1; ++i)
            {
                if (input[i] == '\r' && input[i + 1] == '\n')
                {
                    firstLineLength = i;
                    break;
                }
            }

            var le = input.Take(firstLineLength);
            var line = Encoding.ASCII.GetString(le.ToArray());
            var tokens = line.Split();
            // TODO: Check the number of tokens
            CommandName = tokens[0];
            Key = tokens[1];
            if (CommandName == "set")
            {
                int length = Convert.ToInt32(tokens[4]);
                Data = new byte[length];
                Array.Copy(input, firstLineLength + 2, Data, 0, length);
            }
            else
            {
                Data = new byte[] {};
            }
        }

        public string CommandName { get; private set; }

        public string Key { get; private set; }

        public byte[] Data { get; private set; }
    }
}