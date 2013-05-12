using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service.Eviction;

namespace Nemcache.Tests.Eviction
{
    [TestClass]
    public class NullEvictionTests
    {
        [TestMethod]
        public void EvictDoesNothing()
        {
            var strategy = new NullEvictionStrategy();
            strategy.EvictEntry();
        }
    }
}