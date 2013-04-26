using System;
using System.Collections.Generic;
using Nemcache.Service;

namespace Nemcache.Tests
{
    internal class TestScheduler : IScheduler
    {
        private readonly List<Tuple<DateTime, Action>> _timers = new List<Tuple<DateTime, Action>>();
        private DateTime _clock = new DateTime(1970, 1, 1);

        public DateTime Now
        {
            get { return _clock; }
        }


        public IDisposable Schedule(TimeSpan delay, Action action)
        {
            _timers.Add(Tuple.Create(_clock + delay, action));
            return null;
        }

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
    }
}