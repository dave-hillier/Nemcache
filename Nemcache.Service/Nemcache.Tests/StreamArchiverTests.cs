using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using ProtoBuf;

namespace Nemcache.Tests
{
    [TestClass]
    public class StreamArchiverTests
    {
        private IMemCache _originalCache;
        private StreamArchiver _streamArchiver;
        private MemoryStream _outputStream;

        [TestInitialize]
        public void Setup()
        {
            // TODO: test the archiver without a cache
            _originalCache = new MemCache(1000);
            _outputStream = new MemoryStream();
            _streamArchiver = new StreamArchiver(_outputStream);
            _originalCache.Notifications.Subscribe(_streamArchiver);
        }

        [TestMethod]
        public void EnsureStoreWritesToJournal()
        {
            _originalCache.Store("my_key", 911, new DateTime(2013, 04, 26), Encoding.ASCII.GetBytes("Payload"));

            var output = _outputStream.ToArray();

            var notification = Serializer.DeserializeWithLengthPrefix<StreamArchiver.ArchiveEntry>(
                new MemoryStream(output), PrefixStyle.Fixed32);
            Assert.IsNotNull(notification.Store);
            Assert.AreEqual("my_key", notification.Store.Key);
            // TODO: rest of the data
        }

        [TestMethod]
        public void WriteAndRestoreSingleKey()
        {
            _originalCache.Store("my_key", 911, new DateTime(2013, 04, 26), Encoding.ASCII.GetBytes("Payload"));

            var output = _outputStream.ToArray();

            var newCache = new MemCache(1000);
            StreamArchiver.Restore(new MemoryStream(output), newCache);

            var cacheEntry = newCache.Retrieve(new [] { "my_key" }).ToArray();
            Assert.AreEqual(1, cacheEntry.Length);
            Assert.AreEqual("Payload", Encoding.ASCII.GetString(cacheEntry[0].Value.Data));
        }

        [TestMethod]
        public void WriteAndRestoreTwoKeys()
        {
            _originalCache.Store("my_key1", 123, new DateTime(2013, 05, 26), Encoding.ASCII.GetBytes("Payload1"));
            _originalCache.Store("my_key2", 456, new DateTime(2013, 05, 26), Encoding.ASCII.GetBytes("Payload2"));

            var output = _outputStream.ToArray();

            var newCache = new MemCache(1000);
            StreamArchiver.Restore(new MemoryStream(output), newCache);

            var cacheEntry = newCache.Retrieve(new [] { "my_key1", "my_key2" }).ToArray();
            Assert.AreEqual(2, cacheEntry.Length);
            Assert.AreEqual("Payload1", Encoding.ASCII.GetString(cacheEntry[0].Value.Data));
            Assert.AreEqual("Payload2", Encoding.ASCII.GetString(cacheEntry[1].Value.Data));
        }

        [TestMethod]
        public void CompleteWillDisposeStream()
        {
            _streamArchiver.OnCompleted();
            Assert.IsFalse(_outputStream.CanWrite); // TODO: Replace with a test double?
        }

        [TestMethod]
        public void ErrorWillDisposeStream()
        {
            _streamArchiver.OnError(new Exception());
            Assert.IsFalse(_outputStream.CanWrite); // TODO: Replace with a test double?
        }
    }
}
