﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");

            var response = _requestHandler.Dispatch("remote", setBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void AppendToEmpty()
        {
            var appendBuilder = new MemcacheStorageCommandBuilder("append", "key", "value");

            var response = _requestHandler.Dispatch("", appendBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }


        [TestMethod]
        public void AppendToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");

            _requestHandler.Dispatch("remote", setBuilder.ToRequest());  
          
            var appendBuilder = new MemcacheStorageCommandBuilder("append", "key", "value");

            var response = _requestHandler.Dispatch("", appendBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void GetValueOfAppendToEmpty()
        {
            var appendBuilder = new MemcacheStorageCommandBuilder("append", "key", "value");

            _requestHandler.Dispatch("", appendBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        // TODO: append ignores existing flags and exp

        [TestMethod]
        public void GetValueOfAppendToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "first");
            _requestHandler.Dispatch("remote", setBuilder.ToRequest());

            var appendBuilder = new MemcacheStorageCommandBuilder("append", "key", " second");
            _requestHandler.Dispatch("", appendBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 12\r\nfirst second\r\nEND\r\n", response.ToAsciiString());
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
        public void FlagsSetAndGet()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            storageBuilder.WithFlags(1234567890);
            _requestHandler.Dispatch("remote", storageBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");

            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());

            Assert.AreEqual("VALUE key 1234567890 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void FlagsMaxValueSetAndGet()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            storageBuilder.WithFlags(ulong.MaxValue);
            var request = storageBuilder.ToRequest();
            _requestHandler.Dispatch("remote", request);

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");

            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());
            Assert.AreEqual("VALUE key " + ulong.MaxValue.ToString() + " 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
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

            var storageBuilder2 = new MemcacheStorageCommandBuilder("set", "key2", "222");
            _requestHandler.Dispatch("remote", storageBuilder2.ToRequest());
            
            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key1", "key2");
            var response = _requestHandler.Dispatch("abc", getBuilder.ToRequest());
            
            Assert.AreEqual("VALUE key1 0 6\r\n111111\r\nVALUE key2 0 3\r\n222\r\nEND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void GetNotFound()
        {
            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void IncrNotFound()
        {
            var builder = new MemcacheIncrCommandBuilder("incr", "key", 1);
            var response = _requestHandler.Dispatch("", builder.ToRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }
        [TestMethod]
        public void DecrNotFound()
        {
            var builder = new MemcacheIncrCommandBuilder("decr", "key", 1);
            var response = _requestHandler.Dispatch("", builder.ToRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void Incr()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "123");

            _requestHandler.Dispatch("remote", storageBuilder.ToRequest());

            var builder = new MemcacheIncrCommandBuilder("incr", "key", 1);
            var response = _requestHandler.Dispatch("", builder.ToRequest());

            Assert.AreEqual("124\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void Decr()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "123");

            _requestHandler.Dispatch("remote", storageBuilder.ToRequest());

            var builder = new MemcacheIncrCommandBuilder("decr", "key", 1);
            var response = _requestHandler.Dispatch("", builder.ToRequest());

            Assert.AreEqual("122\r\n", response.ToAsciiString());
        }

        // TODO: incr/decr max and overflow, non-int start value

        [TestMethod]
        public void PrependToEmpty()
        {
            var prependBuilder = new MemcacheStorageCommandBuilder("prepend", "key", "value");

            var response = _requestHandler.Dispatch("", prependBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }


        [TestMethod]
        public void PrependToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");

            _requestHandler.Dispatch("remote", setBuilder.ToRequest());

            var prependBuilder = new MemcacheStorageCommandBuilder("prepend", "key", "value");

            var response = _requestHandler.Dispatch("", prependBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void GetValueOfPrependToEmpty()
        {
            var prependBuilder = new MemcacheStorageCommandBuilder("prepend", "key", "value");

            _requestHandler.Dispatch("", prependBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        // TODO: prepend ignores existing flags and exp

        [TestMethod]
        public void GetValueOfPrependToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "first");
            _requestHandler.Dispatch("remote", setBuilder.ToRequest());

            var prependBuilder = new MemcacheStorageCommandBuilder("prepend", "key", "second ");
            _requestHandler.Dispatch("", prependBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = _requestHandler.Dispatch("", getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 12\r\nsecond first\r\nEND\r\n", response.ToAsciiString());
        }
    }
}
