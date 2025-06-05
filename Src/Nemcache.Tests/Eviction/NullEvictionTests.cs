using NUnit.Framework;
using Nemcache.Service.Eviction;

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