using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    interface IScheduler
    {
        DateTime Now { get; }

        IDisposable Schedule(TimeSpan delay, Action action);
    }
    /// <summary>
    /// A wrapper for system calls to enable no time sensitive tests
    /// </summary>
    class Scheduler : IScheduler
    {
        private static IScheduler _current = new Scheduler();

        public static IScheduler Current
        {
            get { return _current; }
            set { _current = value; }
        }
        public DateTime Now { get { return DateTime.UtcNow; } }

        public IDisposable Schedule(TimeSpan delay, Action action)
        {
            var timer = new Timer(cb => action(), null, delay, TimeSpan.FromMilliseconds(-1));
            return timer;
        }
    }
}
