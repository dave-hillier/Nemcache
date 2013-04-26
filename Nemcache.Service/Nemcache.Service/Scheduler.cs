using System;
using System.Threading;

namespace Nemcache.Service
{
    internal interface IScheduler
    {
        DateTime Now { get; }

        IDisposable Schedule(TimeSpan delay, Action action);
    }

    /// <summary>
    ///     A wrapper for system calls to enable no time sensitive tests
    /// </summary>
    internal class Scheduler : IScheduler
    {
        private static IScheduler _current = new Scheduler();

        public static IScheduler Current
        {
            get { return _current; }
            set { _current = value; }
        }

        public DateTime Now
        {
            get { return DateTime.UtcNow; }
        }

        public IDisposable Schedule(TimeSpan delay, Action action)
        {
            var timer = new Timer(cb => action(), null, delay, TimeSpan.FromMilliseconds(-1));
            return timer;
        }
    }
}