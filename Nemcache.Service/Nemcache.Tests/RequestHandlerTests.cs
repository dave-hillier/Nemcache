using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nemcache.Tests.Builders;
using Nemcache.Service;
using System.Threading;

namespace Nemcache.Tests
{
    // TODO: split this into separate command tests
    [TestClass]
    public class RequestHandlerTests
    {
        RequestHandler _requestHandler;
        TestScheduler _testScheduler;

        [TestInitialize]
        public void Setup()
        {
            _requestHandler = new RequestHandler(100000);
            Scheduler.Current = _testScheduler = new TestScheduler();
        }

        #region capacity tests
        [TestMethod]
        public void StoreInCapacity()
        {
            _requestHandler.Capacity = 10;
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "1234567890");

            var response = Dispatch(setBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void StoreOverCapacity()
        {
            _requestHandler.Capacity = 5;
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "1234567890");

            var response = Dispatch(setBuilder.ToRequest());

            Assert.AreEqual("ERROR Over capacity\r\n", response.ToAsciiString());
        }


        [TestMethod]
        public void StoreEvictOverCapacity()
        {
            _requestHandler.Capacity = 10;
            var setBuilder1 = new MemcacheStorageCommandBuilder("set", "key1", "1234567890");
            var setBuilder2 = new MemcacheStorageCommandBuilder("set", "key2", "1234567890");

            Dispatch(setBuilder1.ToRequest());
            var response = Dispatch(setBuilder2.ToRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key1");
            var response2 = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("END\r\n", response2.ToAsciiString());
        }

        [TestMethod]
        public void StoreMultipleEvictOverCapacity()
        {
            _requestHandler.Capacity = 10;
            var setBuilder1 = new MemcacheStorageCommandBuilder("set", "key1", "12345");
            var setBuilder2 = new MemcacheStorageCommandBuilder("set", "key2", "12345");
            var setBuilder3 = new MemcacheStorageCommandBuilder("set", "key3", "1234567890");

            Dispatch(setBuilder1.ToRequest());
            Dispatch(setBuilder2.ToRequest());
            Dispatch(setBuilder3.ToRequest());

            var getBuilder1 = new MemcacheRetrivalCommandBuilder("get", "key1");
            var response1 = Dispatch(getBuilder1.ToRequest());
            Assert.AreEqual("END\r\n", response1.ToAsciiString());
            var getBuilder2 = new MemcacheRetrivalCommandBuilder("get", "key2");
            var response2 = Dispatch(getBuilder2.ToRequest());
            Assert.AreEqual("END\r\n", response2.ToAsciiString());
        }
        #endregion
        
        #region set and get
        [TestMethod]
        public void Set()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");

            var response = Dispatch(setBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void SetNoReply()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            setBuilder.NoReply();

            var response = Dispatch(setBuilder.ToRequest());

            Assert.AreEqual("", response.ToAsciiString());
        }

        [TestMethod]
        public void SetTwice()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());

            storageBuilder.Data("Updated");
            var response = Dispatch(storageBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void SetThenGet()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void FlagsSetAndGet()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            storageBuilder.WithFlags(1234567890);
            Dispatch(storageBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 1234567890 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void FlagsMaxValueSetAndGet()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            storageBuilder.WithFlags(ulong.MaxValue);
            Dispatch(storageBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");

            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key " + ulong.MaxValue.ToString() + " 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void SetAndSetNewThenGet()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());
            storageBuilder.Data("new value");
            Dispatch(storageBuilder.ToRequest());
            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");

            var response = Dispatch(getBuilder.ToRequest());

            Assert.AreEqual("VALUE key 0 9\r\nnew value\r\nEND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void SetThenGetMultiple()
        {
            var storageBuilder1 = new MemcacheStorageCommandBuilder("set", "key1", "111111");
            Dispatch(storageBuilder1.ToRequest());

            var storageBuilder2 = new MemcacheStorageCommandBuilder("set", "key2", "222");
            Dispatch(storageBuilder2.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key1", "key2");
            var response = Dispatch(getBuilder.ToRequest());

            Assert.AreEqual("VALUE key1 0 6\r\n111111\r\nVALUE key2 0 3\r\n222\r\nEND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void GetNotFound()
        {
            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
        #endregion
        
        #region append tests
        [TestMethod]
        public void AppendToEmpty()
        {
            var appendBuilder = new MemcacheStorageCommandBuilder("append", "key", "value");

            var response = Dispatch(appendBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }


        [TestMethod]
        public void AppendNoReply()
        {
            var appendBuilder = new MemcacheStorageCommandBuilder("append", "key", "value");
            appendBuilder.NoReply();

            var response = Dispatch(appendBuilder.ToRequest());

            Assert.AreEqual("", response.ToAsciiString());
        }


        [TestMethod]
        public void AppendToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(setBuilder.ToRequest());

            var appendBuilder = new MemcacheStorageCommandBuilder("append", "key", "value");
            var response = Dispatch(appendBuilder.ToRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void GetValueOfAppendToEmpty()
        {
            var appendBuilder = new MemcacheStorageCommandBuilder("append", "key", "value");
            Dispatch(appendBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        private byte[] Dispatch(byte[] p)
        {
            return _requestHandler.Dispatch("", p, null); 
        }

        // TODO: append ignores existing flags and exp

        [TestMethod]
        public void GetValueOfAppendToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "first");
            Dispatch(setBuilder.ToRequest());

            var appendBuilder = new MemcacheStorageCommandBuilder("append", "key", " second");
            Dispatch(appendBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 12\r\nfirst second\r\nEND\r\n", response.ToAsciiString());
        }
        #endregion
        
        #region incr and decr
        [TestMethod]
        public void IncrNotFound()
        {
            var builder = new MemcacheIncrCommandBuilder("incr", "key", 1);
            var response = Dispatch(builder.ToRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }
        [TestMethod]
        public void DecrNotFound()
        {
            var builder = new MemcacheIncrCommandBuilder("decr", "key", 1);
            var response = Dispatch(builder.ToRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void Incr()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "123");
            Dispatch(storageBuilder.ToRequest());
            var builder = new MemcacheIncrCommandBuilder("incr", "key", 1);
            var response = Dispatch(builder.ToRequest());
            Assert.AreEqual("124\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void Decr()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "123");
            Dispatch(storageBuilder.ToRequest());
            var builder = new MemcacheIncrCommandBuilder("decr", "key", 1);
            var response = Dispatch(builder.ToRequest());
            Assert.AreEqual("122\r\n", response.ToAsciiString());
        }

        // TODO: incr/decr max and overflow, non-int start value
#endregion
        
        #region prepend
        [TestMethod]
        public void PrependToEmpty()
        {
            var prependBuilder = new MemcacheStorageCommandBuilder("prepend", "key", "value");

            var response = Dispatch(prependBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void PrependNoReply()
        {
            var prependBuilder = new MemcacheStorageCommandBuilder("prepend", "key", "value");
            prependBuilder.NoReply();

            var response = Dispatch(prependBuilder.ToRequest());

            Assert.AreEqual("", response.ToAsciiString());
        }

        [TestMethod]
        public void PrependToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");

            Dispatch(setBuilder.ToRequest());

            var prependBuilder = new MemcacheStorageCommandBuilder("prepend", "key", "value");

            var response = Dispatch(prependBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void GetValueOfPrependToEmpty()
        {
            var prependBuilder = new MemcacheStorageCommandBuilder("prepend", "key", "value");

            Dispatch(prependBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        // TODO: prepend ignores existing flags and exp

        [TestMethod]
        public void GetValueOfPrependToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "first");
            Dispatch(setBuilder.ToRequest());

            var prependBuilder = new MemcacheStorageCommandBuilder("prepend", "key", "second ");
            Dispatch(prependBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 12\r\nsecond first\r\nEND\r\n", response.ToAsciiString());
        }
        #endregion
        
        #region delete

        [TestMethod]
        public void DeleteNotFound()
        {
            var delBuilder = new MemcacheDeleteCommandBuilder("key");
            var response = Dispatch(delBuilder.ToRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void DeleteExisting()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());

            var delBuilder = new MemcacheDeleteCommandBuilder("key");
            var response = Dispatch(delBuilder.ToRequest());

            Assert.AreEqual("DELETED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void DeleteExistingGetNotFound()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());

            var delBuilder = new MemcacheDeleteCommandBuilder("key");
            Dispatch(delBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
        #endregion
        
        #region expiry
        // TODO: Remove time sensitive element
        [TestMethod]
        public void SetExpiryThenGet()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            storageBuilder.WithExpiry(100);

            Dispatch(storageBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        // TODO: Remove time sensitive element
        [TestMethod]
        public void SetExpiryThenGetGone()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            storageBuilder.WithExpiry(1);

            Dispatch(storageBuilder.ToRequest());

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(2));

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
#endregion
        
        #region touch
        [TestMethod]
        public void TouchOk()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            storageBuilder.WithExpiry(100);
            Dispatch(storageBuilder.ToRequest());

            var touchBuilder = new MemcacheTouchCommandBuilder("key");
            touchBuilder.WithExpiry(1);
            var response = Dispatch(touchBuilder.ToRequest());

            Assert.AreEqual("OK\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void TouchNotFound()
        {
            var touchBuilder = new MemcacheTouchCommandBuilder("key");
            touchBuilder.WithExpiry(1);
            var response = Dispatch(touchBuilder.ToRequest());

            Assert.AreEqual("NOT_FOUND\r\n", response.ToAsciiString());
        }

        // TODO: Remove time sensitive element
        [TestMethod]
        public void SetTouchExpiryThenGetGone()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            storageBuilder.WithExpiry(100);
            Dispatch(storageBuilder.ToRequest());
            
            var touchBuilder = new MemcacheTouchCommandBuilder("key");
            touchBuilder.WithExpiry(1);
            Dispatch(touchBuilder.ToRequest());
            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(2));

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
#endregion
        
        #region Add

        [TestMethod]
        public void AddToEmpty()
        {
            var addBuilder = new MemcacheStorageCommandBuilder("add", "key", "value");

            var response = Dispatch(addBuilder.ToRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void AddNoReply()
        {
            var addBuilder = new MemcacheStorageCommandBuilder("add", "key", "value");
            addBuilder.NoReply();

            var response = Dispatch(addBuilder.ToRequest());

            Assert.AreEqual("", response.ToAsciiString());
        }

        [TestMethod]
        public void AddToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");

            Dispatch(setBuilder.ToRequest());

            var addBuilder = new MemcacheStorageCommandBuilder("add", "key", "value");

            var response = Dispatch(addBuilder.ToRequest());

            Assert.AreEqual("NOT_STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void GetValueOfAddToEmpty()
        {
            var addBuilder = new MemcacheStorageCommandBuilder("add", "key", "value");

            Dispatch(addBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }

        #endregion
        
        #region Replace

        [TestMethod]
        public void ReplaceToEmpty()
        {
            var replaceBuilder = new MemcacheStorageCommandBuilder("replace", "key", "value");

            var response = Dispatch(replaceBuilder.ToRequest());

            Assert.AreEqual("NOT_STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void ReplaceNoReply()
        {
            var replaceBuilder = new MemcacheStorageCommandBuilder("replace", "key", "value");
            replaceBuilder.NoReply();

            var response = Dispatch(replaceBuilder.ToRequest());

            Assert.AreEqual("", response.ToAsciiString());
        }

        [TestMethod]
        public void ReplaceToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(setBuilder.ToRequest());

            var replaceBuilder = new MemcacheStorageCommandBuilder("replace", "key", "value");
            var response = Dispatch(replaceBuilder.ToRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        // TODO: replace ignores existing flags and exp

        [TestMethod]
        public void GetValueOfReplaceToExisting()
        {
            var setBuilder = new MemcacheStorageCommandBuilder("set", "key", "first");
            Dispatch(setBuilder.ToRequest());

            var replaceBuilder = new MemcacheStorageCommandBuilder("replace", "key", "second");
            Dispatch(replaceBuilder.ToRequest());

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 6\r\nsecond\r\nEND\r\n", response.ToAsciiString());
        }
        #endregion

        #region flush
        [TestMethod]
        public void FlushResponse()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all\r\n");
            var response = _requestHandler.Dispatch("remote", flushRequest, null);

            Assert.AreEqual("OK\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void FlushDelayResponse()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all 123\r\n");
            var response = _requestHandler.Dispatch("remote", flushRequest, null);

            Assert.AreEqual("OK\r\n", response.ToAsciiString());
        }
        
        [TestMethod]
        public void FlushClearsCache()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all\r\n");
            _requestHandler.Dispatch("remote", flushRequest, null);

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
        [TestMethod]
        public void FlushClearsCacheMultiple()
        {
            var storageBuilder1 = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder1.ToRequest());
            var storageBuilder2 = new MemcacheStorageCommandBuilder("set", "key2", "value");
            Dispatch(storageBuilder2.ToRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all\r\n");
            _requestHandler.Dispatch("remote", flushRequest, null);

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void FlushWithDelayNoEffect()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all 100\r\n");
            _requestHandler.Dispatch("remote", flushRequest, null);

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(90));

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }


        [TestMethod]
        public void FlushWithDelayEmpty()
        {
            var storageBuilder = new MemcacheStorageCommandBuilder("set", "key", "value");
            Dispatch(storageBuilder.ToRequest());

            var flushRequest = Encoding.ASCII.GetBytes("flush_all 100\r\n");
            _requestHandler.Dispatch("remote", flushRequest, null);

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(200));

            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var response = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("END\r\n", response.ToAsciiString());
        }
        #endregion

        #region Cas

        // TODO: cas capacity checks
        [TestMethod]
        public void CasNoPrevious()
        {
            var casBuilder = new MemcacheCasCommandBuilder("key", "value");
            ulong lastCas = 123;
            casBuilder.WithCasUnique(lastCas);

            var response = Dispatch(casBuilder.ToRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());

            // TODO: split test
            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var getResponse = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 5 123\r\nvalue\r\nEND\r\n", getResponse.ToAsciiString());
        }

        [TestMethod]
        public void CasUpdatePrevious()
        {
            var casBuilder1 = new MemcacheCasCommandBuilder("key", "value1");
            casBuilder1.WithCasUnique(567);
            Dispatch(casBuilder1.ToRequest());

            var casBuilder2 = new MemcacheCasCommandBuilder("key", "value2");
            casBuilder2.WithCasUnique(567);
            
            var response2 = Dispatch(casBuilder2.ToRequest());

            Assert.AreEqual("STORED\r\n", response2.ToAsciiString());

            // TODO: split test
            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var getResponse = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 6 567\r\nvalue2\r\nEND\r\n", getResponse.ToAsciiString());
        }


        [TestMethod]
        public void CasUpdatePreviousModified()
        {
            var casBuilder1 = new MemcacheCasCommandBuilder("key", "value1");
            casBuilder1.WithCasUnique(789);
            Dispatch(casBuilder1.ToRequest());

            var casBuilder2 = new MemcacheCasCommandBuilder("key", "value2");
            casBuilder2.WithCasUnique(567);

            var response2 = Dispatch(casBuilder2.ToRequest());
            Assert.AreEqual("EXISTS\r\n", response2.ToAsciiString());

            // TODO: and not changed
            // TODO: split test
            var getBuilder = new MemcacheRetrivalCommandBuilder("get", "key");
            var getResponse = Dispatch(getBuilder.ToRequest());
            Assert.AreEqual("VALUE key 0 6 789\r\nvalue1\r\nEND\r\n", getResponse.ToAsciiString());
        }


        #endregion

        [TestMethod]
        public void Version()
        {
            var flushRequest = Encoding.ASCII.GetBytes("version\r\n");
            var response = _requestHandler.Dispatch("remote", flushRequest, null);
            Assert.AreEqual("Nemcache 1.0.0.0\r\n", response.ToAsciiString());
        }
    }    
}
