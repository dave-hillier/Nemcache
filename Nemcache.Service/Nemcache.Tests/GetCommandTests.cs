using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nemcache.Tests
{
    [TestClass]
    public class GetCommandTests
    {
        [TestMethod]
        public void Test()
        {
            var getRequest = new MemcacheGetCommandBuilder("get", "key").ToRequest();
        }

        // TODO: multi get
        // TODO: gets
    }
}