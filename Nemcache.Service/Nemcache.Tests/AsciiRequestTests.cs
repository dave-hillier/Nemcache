using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using Nemcache.Tests.Builders;

namespace Nemcache.Tests
{
    [TestClass]
    public class AsciiRequestTests
    {
        [TestMethod]
        public void CreateSetRequest()
        {
            var input = new MemcacheStorageCommandBuilder("set", "some_key", new byte[] { 1, 2, 3, 4 }).ToRequest();

            var request = new AsciiRequest(input);

            Assert.AreEqual("set", request.CommandName);
            Assert.AreEqual("some_key", request.Key);
            Assert.IsTrue(new byte[] { 1, 2, 3, 4 }.SequenceEqual(request.Data));
        }

        [TestMethod]
        public void CreateSetRequestWithBigBuffer()
        {
            var inputSmall = new MemcacheStorageCommandBuilder("set", "some_key", new byte[] { 1, 2, 3, 4 }).ToRequest();
            var input = inputSmall.Concat(new byte[10]).ToArray();
            var request = new AsciiRequest(input);

            Assert.AreEqual("set", request.CommandName);
            Assert.AreEqual("some_key", request.Key);
            Assert.IsTrue(new byte[] { 1, 2, 3, 4 }.SequenceEqual(request.Data));
        }

        [TestMethod]
        public void CreateGetRequest()
        {
            var input = new MemcacheRetrivalCommandBuilder("get", "some_key").ToRequest();
            var request = new AsciiRequest(input);

            Assert.AreEqual("get", request.CommandName);
            Assert.AreEqual("some_key", request.Key);
        }

        [TestMethod]
        public void CreateDeleteRequest()
        {
            var input = Encoding.ASCII.GetBytes("delete key_to_be_deleted\r\n");
            var request = new AsciiRequest(input);

            Assert.AreEqual("delete", request.CommandName);
            Assert.AreEqual("key_to_be_deleted", request.Key);
        }

        [TestMethod]
        public void CreatIncrRequest()
        {
            var input = Encoding.ASCII.GetBytes("incr key_to_be_deleted 4\r\n"); 
            var request = new AsciiRequest(input);

            Assert.AreEqual("incr", request.CommandName);
            Assert.AreEqual("key_to_be_deleted", request.Key);
            Assert.AreEqual((ulong)4, request.Value);
        }

        [TestMethod]
        public void CreatDecrRequest()
        {
            var input = Encoding.ASCII.GetBytes("decr keykeykeeeeey 21\r\n");
            var request = new AsciiRequest(input);

            Assert.AreEqual("decr", request.CommandName);
            Assert.AreEqual("keykeykeeeeey", request.Key);
            Assert.AreEqual((ulong)21, request.Value);
        }

        // TODO: various invalidequest
        // TODO: various flags
        // TODO: seconds expiry
        // TODO: unix time expiry
        // TODO: get requests
    }
}