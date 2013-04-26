using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    internal class NullEvictionStrategy : IEvictionStrategy
    {
        public void EvictEntry()
        {
        }
    }
}
