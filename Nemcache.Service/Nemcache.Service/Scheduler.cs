using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    interface IScheduler
    {
        DateTime Now { get; }
    }

    class Scheduler : IScheduler
    {
        private static IScheduler _current = new Scheduler();
        public static IScheduler Current
        {
            get { return _current; }
            set { _current = value; }
        }
        public DateTime Now { get { return DateTime.UtcNow; } }
    }
}
