using Nemcache.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nemcache.Tests
{
    class TestScheduler : IScheduler
    {
        private DateTime _clock = new DateTime();
        private List<Tuple<DateTime, Action>> _timers = new List<Tuple<DateTime, Action>>();
        public void AdvanceBy(TimeSpan timespan)
        {
            _clock += timespan;

            var timersCopy = _timers.ToArray();
            foreach (var timer in timersCopy)
            {
                if (timer.Item1 < _clock)
                {
                    timer.Item2();
                    _timers.Remove(timer);
                }
            }
        }

        public DateTime Now
        {
            get { return _clock; }
        }


        public IDisposable Schedule(TimeSpan delay, Action action)
        {
            _timers.Add(Tuple.Create(_clock+delay, action));
            return null;
        }
    }
}
