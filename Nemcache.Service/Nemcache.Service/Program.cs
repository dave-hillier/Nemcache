using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class Program
    {
        static void Main(string[] args)
        {
            var commands = new Dictionary<string, ICommand>();
            var cache = new ArrayMemoryCache();
            var get = new GetCommand(cache);
            var set = new SetCommand(cache);
            commands.Add(get.Name, get);
            commands.Add(set.Name, set);


        }
    }
}