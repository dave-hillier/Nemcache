using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using Nemcache.Service.Persistence;
using ProtoBuf;
using System;
using System.IO;
using System.Text;

namespace Nemcache.Tests.Persistence
{
    [TestClass]
    public class StreamArchiverTests
    {
        private IMemCache _originalCache;
        private MemoryStream _outputStream;
        private StreamArchiver _streamArchiver;

        [TestInitialize]
        public void Setup()
        {
            // TODO: test the archiver without a cache
            _originalCache = new MemCache(1000);
            _outputStream = new MemoryStream();
            _streamArchiver = new StreamArchiver(_outputStream);
            _originalCache.FullStateNotifications.Subscribe(_streamArchiver);
        }

        [TestMethod]
        public void EnsureStoreWritesToJournal()
        {
            _originalCache.Store("my_key", 911, Encoding.ASCII.GetBytes("Payload"), new DateTime(2013, 04, 26));

            var output = _outputStream.ToArray();

            var notification = Serializer.DeserializeWithLengthPrefix<ArchiveEntry>(
                new MemoryStream(output), PrefixStyle.Fixed32);
            Assert.IsNotNull(notification.Store);
            Assert.AreEqual("my_key", notification.Store.Key);
            // TODO: rest of the data
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