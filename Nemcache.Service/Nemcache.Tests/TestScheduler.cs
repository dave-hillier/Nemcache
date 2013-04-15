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

        public void AdvanceBy(TimeSpan timespan)
        {
            _clock += timespan;
        }

        public DateTime Now
        {
            get { return _clock; }
        }
    }
}
