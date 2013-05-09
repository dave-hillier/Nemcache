using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nemcache.Service;
using Nemcache.Service.IO;
using Nemcache.Service.Notifications;
using ProtoBuf;

namespace Nemcache.Tests
{
    [TestClass]
    public class CacheRestorerTests
    {
        private Mock<IFileSystem> _mockFileSystem;
        private Mock<IFile> _mockFile;
        private Mock<IMemCache> _mockCache;
        private CacheRestorer _cacheRestorer;
        private MemoryStream _readStream;
        private MemoryStream _writeStream;

        [TestInitialize]
        public void Setup()
        {
            _mockCache = new Mock<IMemCache>();
            _mockFile = new Mock<IFile>();

            _readStream = new MemoryStream();
            _writeStream = new MemoryStream();
            _mockFile.Setup(s => s.Open(
                It.IsAny<string>(),
                It.Is<FileMode>(fm => fm == FileMode.OpenOrCreate),
                It.Is<FileAccess>(fa => fa == FileAccess.Write))).Returns(_writeStream);

            _mockFileSystem = new Mock<IFileSystem>();
            _mockFileSystem.SetupGet(fs => fs.File).Returns(_mockFile.Object);

            _cacheRestorer = new CacheRestorer(_mockCache.Object, _mockFileSystem.Object, "log");
        }

        [TestMethod]
        public void NoLogFile()
        {
            GivenNoLogFile();

            _cacheRestorer.RestoreCache();

            // Then nothing happens
            _mockCache.Verify(c => c.Add(It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<DateTime>(), It.IsAny<byte[]>()), Times.Never());
            _mockCache.Verify(c => c.Store(It.IsAny<string>(), It.IsAny<ulong>(),  It.IsAny<byte[]>(), It.IsAny<DateTime>()), Times.Never());
        }

        [TestMethod]
        public void KeyIsAdded()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.Verify(c => c.Add(It.Is<string>(key => key == "key"), 
                It.IsAny<ulong>(), It.IsAny<DateTime>(), It.IsAny<byte[]>()), Times.Once());
        }

        [TestMethod]
        public void TouchedIsSaved()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.Verify(c => c.Add(It.Is<string>(key => key == "touched"),
                It.IsAny<ulong>(), 
                It.Is<DateTime>(dt => dt == DateTime.MaxValue), 
                It.IsAny<byte[]>()), Times.Once());
        }


        [TestMethod]
        public void DontRestoreDeleted()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.Verify(c => c.Add(It.Is<string>(key => key == "deleted"),
                It.IsAny<ulong>(), It.IsAny<DateTime>(), It.IsAny<byte[]>()), Times.Never());
        }

        [TestMethod]
        public void DontRestoreExpired()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.Verify(c => c.Add(It.Is<string>(key => key == "expired"),
                It.IsAny<ulong>(), It.IsAny<DateTime>(), It.IsAny<byte[]>()), Times.Never());
        }

        [TestMethod]
        public void DontRestoreBeforeClear()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.Verify(c => c.Add(It.Is<string>(key => key == "pre-clear"),
                It.IsAny<ulong>(), It.IsAny<DateTime>(), It.IsAny<byte[]>()), Times.Never());
        }

        [TestMethod]
        public void OnlyLastUpdate()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.Verify(c => c.Add(It.Is<string>(key => key == "updated"),
                It.IsAny<ulong>(), It.IsAny<DateTime>(), 
                It.Is<byte[]>(bytes => bytes.SequenceEqual(new byte[] {4, 5, 6, 7, 8}))), Times.Once());
        }



        [TestMethod]
        public void LogIsReplacedWithCompacted() 
        {
            // TODO: test that temp file name is same in open and replace
            _mockFile.Setup(s => s.Open(
                It.IsAny<string>(),
                It.Is<FileMode>(fm => fm == FileMode.OpenOrCreate),
                It.Is<FileAccess>(fa => fa == FileAccess.Write))).Returns(_writeStream);

            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockFile.Verify(file => file.Replace(
                It.IsAny<string>(), 
                It.Is<string>(d => d == "log"), 
                It.IsAny<string>(), 
                It.IsAny<bool>()), Times.Once());

            var entries = CacheRestorer.ReadLog(new MemoryStream(_writeStream.ToArray()));
            Assert.AreEqual(3, entries.Count());
        }

        // TODO: touch entries
        // TODO: expiry

        private static MemoryStream CreateLogStream()
        {
            var memoryStream = new MemoryStream();

            var entries = new[] { 
                new ArchiveEntry
                {
                    Store = new StoreNotification
                        {
                            Key = "pre-clear",
                            Operation = StoreOperation.Add,
                            Data = new byte[] {1, 2, 3},
                            Expiry = DateTime.MaxValue,
                            EventId = 1
                        }
                },
                new ArchiveEntry
                {
                    Clear = new ClearNotification(),
                },
                new ArchiveEntry
                {
                    Store = new StoreNotification
                        {
                            Key = "key",
                            Operation = StoreOperation.Add,
                            Data = new byte[] {1, 2, 3},
                            Expiry = DateTime.MaxValue,
                            EventId = 1
                        }
                },
                new ArchiveEntry
                {
                    Store = new StoreNotification
                        {
                            Key = "updated",
                            Operation = StoreOperation.Add,
                            Data = new byte[] {1, 2, 3},
                            Expiry = DateTime.MaxValue,
                            EventId = 2
                        }
                },
                new ArchiveEntry
                {
                    Store = new StoreNotification
                        {
                            Key = "updated",
                            Operation = StoreOperation.Store,
                            Data = new byte[] {4, 5, 6, 7, 8},
                            Expiry = DateTime.MaxValue,
                            EventId = 3
                        }
                },
                new ArchiveEntry
                {
                    Store = new StoreNotification
                        {
                            Key = "deleted",
                            Operation = StoreOperation.Add,
                            Data = new [] {(byte)'a', (byte)'b', (byte)'c'},
                            Expiry = DateTime.MaxValue,
                            EventId = 4
                        }
                },
                new ArchiveEntry
                {
                    Remove = new RemoveNotification
                        {
                            Key = "deleted",
                            EventId = 5
                        }
                },
                new ArchiveEntry
                {
                    Store = new StoreNotification
                        {
                            Key = "expired",
                            Operation = StoreOperation.Store,
                            Data = new byte[] {10, 11, 12, 13, 14, 15, 16},
                            Expiry = DateTime.MinValue,
                            EventId = 6
                        }
                },
                new ArchiveEntry
                {
                    Store = new StoreNotification
                        {
                            Key = "touched",
                            Operation = StoreOperation.Store,
                            Data = new byte[] {10, 11, 12, 13, 14, 15, 16},
                            Expiry = DateTime.MinValue,
                            EventId = 6
                        }
                },
                new ArchiveEntry
                {
                    Touch  = new TouchNotification
                        {
                            Expiry = DateTime.MaxValue,
                            EventId = 7,
                            Key = "touched"
                        }
                }

            };
            foreach (var archiveEntry in entries)
            {
                Serializer.SerializeWithLengthPrefix(memoryStream, archiveEntry, PrefixStyle.Fixed32);                
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }


        private void GivenNoLogFile()
        {
            _mockFile.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
            _mockFile.Setup(f => f.Open(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>()))
                     .Throws(new FileNotFoundException());
        }


        private void GivenLogFile(string name, Stream contents)
        {
            _mockFile.Setup(f => f.Exists(It.Is<string>(s => s == name))).Returns(true);
            _mockFile.Setup(f => f.Open(It.Is<string>(fn => fn == name), It.Is<FileMode>(fm => fm == FileMode.Open), It.Is<FileAccess>(fa => fa == FileAccess.Read)))
                     .Returns(contents);
        }
    }
}
