using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Nemcache.Service;
using Nemcache.Service.IO;
using Nemcache.Service.Notifications;
using Nemcache.Service.Persistence;
using ProtoBuf;

namespace Nemcache.Tests.Persistence
{
    [TestClass]
    public class CacheRestorerTests
    {
        private IFileSystem _mockFileSystem;
        private IFile _mockFile;
        private IMemCache _mockCache;
        private CacheRestorer _cacheRestorer;
        private MemoryStream _readStream;
        private MemoryStream _writeStream;

        [TestInitialize]
        public void Setup()
        {
            _mockCache = Substitute.For<IMemCache>();
            _mockFile = Substitute.For<IFile>();

            _readStream = new MemoryStream();
            _writeStream = new MemoryStream();
            _mockFile.Open(Arg.Any<string>(), FileMode.OpenOrCreate, FileAccess.Write)
                     .Returns(_writeStream);

            _mockFileSystem = Substitute.For<IFileSystem>();
            _mockFileSystem.File.Returns(_mockFile);

            _cacheRestorer = new CacheRestorer(_mockCache, _mockFileSystem, "log");
        }

        [TestMethod]
        public void NoLogFile()
        {
            GivenNoLogFile();

            _cacheRestorer.RestoreCache();

            // Then nothing happens
            _mockCache.DidNotReceive().Add(Arg.Any<string>(), Arg.Any<ulong>(), Arg.Any<DateTime>(), Arg.Any<byte[]>());
            _mockCache.DidNotReceive().Store(Arg.Any<string>(), Arg.Any<ulong>(), Arg.Any<byte[]>(), Arg.Any<DateTime>());
        }

        [TestMethod]
        public void KeyIsAdded()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.Received(1).Add(Arg.Is<string>(key => key == "key"),
                Arg.Any<ulong>(), Arg.Any<DateTime>(), Arg.Any<byte[]>());
        }

        [TestMethod]
        public void TouchedIsSaved()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.Received(1).Add(Arg.Is<string>(key => key == "touched"),
                Arg.Any<ulong>(),
                Arg.Is<DateTime>(dt => dt == DateTime.MaxValue),
                Arg.Any<byte[]>());
        }


        [TestMethod]
        public void DontRestoreDeleted()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.DidNotReceive().Add(Arg.Is<string>(key => key == "deleted"),
                Arg.Any<ulong>(), Arg.Any<DateTime>(), Arg.Any<byte[]>());
        }

        [TestMethod]
        public void DontRestoreExpired()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.DidNotReceive().Add(Arg.Is<string>(key => key == "expired"),
                Arg.Any<ulong>(), Arg.Any<DateTime>(), Arg.Any<byte[]>());
        }

        [TestMethod]
        public void DontRestoreBeforeClear()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.DidNotReceive().Add(Arg.Is<string>(key => key == "pre-clear"),
                Arg.Any<ulong>(), Arg.Any<DateTime>(), Arg.Any<byte[]>());
        }

        [TestMethod]
        public void OnlyLastUpdate()
        {
            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockCache.Received(1).Add(Arg.Is<string>(key => key == "updated"),
                Arg.Any<ulong>(), Arg.Any<DateTime>(),
                Arg.Is<byte[]>(bytes => bytes.SequenceEqual(new byte[] {4, 5, 6, 7, 8})));
        }



        [TestMethod]
        public void LogIsReplacedWithCompacted() 
        {
            // TODO: test that temp file name is same in open and replace
            _mockFile.Open(Arg.Any<string>(), FileMode.OpenOrCreate, FileAccess.Write)
                     .Returns(_writeStream);

            GivenLogFile("log", CreateLogStream());

            _cacheRestorer.RestoreCache();

            _mockFile.Received(1).Replace(
                Arg.Any<string>(),
                Arg.Is<string>(d => d == "log"),
                Arg.Any<string>(),
                Arg.Any<bool>());

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
            _mockFile.Exists(Arg.Any<string>()).Returns(false);
            _mockFile
                .When(f => f.Open(Arg.Any<string>(), Arg.Any<FileMode>(), Arg.Any<FileAccess>()))
                .Do(_ => { throw new FileNotFoundException(); });
        }


        private void GivenLogFile(string name, Stream contents)
        {
            _mockFile.Exists(Arg.Is<string>(s => s == name)).Returns(true);
            _mockFile.Open(Arg.Is<string>(fn => fn == name), FileMode.Open, FileAccess.Read)
                     .Returns(contents);
        }
    }
}
