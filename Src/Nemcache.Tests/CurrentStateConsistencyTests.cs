using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Nemcache.Storage;

namespace Nemcache.Tests
{
    [TestFixture]
    public class CurrentStateConsistencyTests
    {
        [Test]
        public void SnapshotEventIdAlwaysCoversEntries()
        {
            var cache = new MemCache(10000);

            var writer = Task.Run(() =>
            {
                for (int i = 0; i < 500; i++)
                {
                    cache.Store("k" + i, 0, Encoding.ASCII.GetBytes("v"), DateTime.MaxValue);
                }
            });

            while (!writer.IsCompleted)
            {
                var state = cache.CurrentState;
                foreach (var entry in state.Item2)
                {
                    Assert.LessOrEqual(entry.Value.EventId, state.Item1);
                }
            }

            // final check
            var finalState = cache.CurrentState;
            foreach (var entry in finalState.Item2)
            {
                Assert.LessOrEqual(entry.Value.EventId, finalState.Item1);
            }
        }
    }
}
