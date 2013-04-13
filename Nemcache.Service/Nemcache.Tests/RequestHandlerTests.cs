using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nemcache.Tests.Builders;
using Nemcache.Service;

namespace Nemcache.Tests
{
    public static class HelperExt
    {
        public static string ToAsciiString(this byte[] s)
        {
            return Encoding.ASCII.GetString(s);
        }
    }

    [TestClass]
    public class RequestHandlerTests
    {
        Program _requestHandler;
        [TestInitialize]
        public void Setup()
        {
            _requestHandler = new Program();
        }
        
        [TestMethod]
        public void Set()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");

            var response = _requestHandler.Dispatch("remote", storageBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void SetTwice()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");

            _requestHandler.Dispatch("remote", storageBuilder.ToRequest());
            storageBuilder.Data("Updated");
            var response = _requestHandler.Dispatch("remote", storageBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void SetThenGet()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");

            _requestHandler.Dispatch("remote", storageBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");

            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());

            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void SetAndSetNewThenGet()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            _requestHandler.Dispatch("remote", storageBuilder.ToRequest());
            storageBuilder.Data("new value");
            _requestHandler.Dispatch("remote", storageBuilder.ToRequest());
            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");

            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());

            Assert.AreEqual("VALUE key 0 9\r\nnew value\r\nEND\r\n", response.ToAsciiString());
        }
        
        [TestMethod]
        public void SetThenGetMultiple()
        {
            var storageBuilder1 = new MemcacheStorageCommandBuilder("set", "key1", "111111");
            _requestHandler.Dispatch("remote", storageBuilder1.ToRequest());

            var storageBuilder2 = new MemcacheStorageCommandBuilder("set", "key2", "222222");
            _requestHandler.Dispatch("remote", storageBuilder2.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key1", "key2");

            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());

            // TODO: Ordering unimportant?
            Assert.AreEqual("VALUE 0 0 0\r\n111111\r\nVALUE 0 0 0\r\n222222\r\nEND\r\n", response.ToAsciiString());
        }


        [TestMethod]
        public void GetNotFound()
        {
            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");

            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());

            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void Test3()
        {
        }
    }

}
