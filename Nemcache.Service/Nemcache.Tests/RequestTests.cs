using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class RequestTests
    {
        [TestMethod]
        public void CreateRequest()
        {
            var input = new MemcacheCommandBuilder("set", "some_key", new byte[] { 1, 2, 3, 4 }).ToRequest();

            var request = new Request(input);

            Assert.AreEqual("set", request.Command);
            Assert.AreEqual("some_key", request.Key);
            Assert.IsTrue(new byte[] { 1, 2, 3, 4 }.SequenceEqual(request.Data));
        }
    }
}