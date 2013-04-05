using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class SetCommand
    {
        public string Name { get { return "set"; } }

        public byte[] Execute(IRequest request)
        {
            return Encoding.ASCII.GetBytes("STORED\r\n");
        }
    }
}
