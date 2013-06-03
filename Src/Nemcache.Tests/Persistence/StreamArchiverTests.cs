using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nemcache.Service;
using Nemcache.Service.IO;
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
        private MemCache _originalCache;
        private StreamArchiver _streamArchiver;
        private Mock<IFile> _mockFile;
        private MemoryStream _writeStream;
        private Mock<IFileSystem> _mockFileSystem;

        [TestInitialize]
        public void Setup()
        {
            // TODO: test the archiver without a cache
            _originalCache = new MemCache(1000);
            _mockFile = new Mock<IFile>();

            _writeStream = new MemoryStream();
            _mockFile.Setup(s => s.Open(
                It.IsAny<string>(),
                It.Is<FileMode>(fm => fm == FileMode.OpenOrCreate),
                It.Is<FileAccess>(fa => fa == FileAccess.Write))).Returns(_writeStream);

            _mockFileSystem = new Mock<IFileSystem>();
            _mockFileSystem.SetupGet(fs => fs.File).Returns(_mockFile.Object);


            _streamArchiver = new StreamArchiver(_mockFileSystem.Object, "path", _originalCache, 1000);
            _originalCache.Notifications.Subscribe(_streamArchiver);
        }

        [TestMethod]
        public void EnsureStoreWritesToJournal()
        {
            _originalCache.Store("my_key", 911, Encoding.ASCII.GetBytes("Payload"), new DateTime(2013, 04, 26));

            var output = _writeStream.ToArray();

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
            Assert.IsFalse(_writeStream.CanWrite); // TODO: Replace with a test double?
        }

        [TestMethod]
        public void ErrorWillDisposeStream()
        {
            _streamArchiver.OnError(new Exception());
            Assert.IsFalse(_writeStream.CanWrite); // TODO: Replace with a test double?
        }

        // 
    }
}