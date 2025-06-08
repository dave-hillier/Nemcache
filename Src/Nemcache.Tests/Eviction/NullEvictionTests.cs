using NUnit.Framework;
using Nemcache.Storage.Eviction;

namespace Nemcache.Tests.Eviction
{
    [TestFixture]
    public class NullEvictionTests
    {
        [Test]
        public void EvictDoesNothing()
        {
            var strategy = new NullEvictionStrategy();
            strategy.EvictEntry();
        }
    }
}