using System;
using System.IO;
using NUnit.Framework;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;

namespace Nemcache.Tests.Persistence
{
    [TestFixture]
    public class HybridLogStoreTests
    {
        private string _dir = null!;
        private IFileSystem _fs = null!;

        private class SharedFileSystem : IFileSystem, IFile
        {
            public IFile File => this;
            public Stream Open(string path, FileMode mode, FileAccess access) => new FileStream(path, mode, access, FileShare.ReadWrite);
            public bool Exists(string path) => File.Exists(path);
            public long Size(string filename) => new FileInfo(filename).Length;
            public void Delete(string path) => File.Delete(path);
            public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
                => File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
        }

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_dir);
            _fs = new SharedFileSystem();
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        [Test]
        public void PutAndGetRoundtrip()
        {
            var path = Path.Combine(_dir, "log.dat");
            using (var store = new HybridLogStore(_fs, path, 1024))
            {
                store.Put("k", new byte[] {1,2,3});
                Assert.IsTrue(store.TryGet("k", out var val));
                CollectionAssert.AreEqual(new byte[] {1,2,3}, val);
            }

            using (var store = new HybridLogStore(_fs, path, 1024))
            {
                Assert.IsTrue(store.TryGet("k", out var value));
                CollectionAssert.AreEqual(new byte[] {1,2,3}, value);
            }
        }

        [Test]
        public void FlushesToDiskWhenLimitExceeded()
        {
            var path = Path.Combine(_dir, "log.dat");
            using (var store = new HybridLogStore(_fs, path, 10))
            {
                store.Put("a", new byte[] {4,5,6,7,8,9}); // exceed limit
            }

            using (var store = new HybridLogStore(_fs, path, 10))
            {
                Assert.IsTrue(store.TryGet("a", out var value));
                CollectionAssert.AreEqual(new byte[] {4,5,6,7,8,9}, value);
            }
        }

        [Test]
        public void DeleteRemovesItem()
        {
            var path = Path.Combine(_dir, "log.dat");
            using (var store = new HybridLogStore(_fs, path, 100))
            {
                store.Put("z", new byte[] {9});
                store.Delete("z");
                Assert.IsFalse(store.TryGet("z", out _));
            }
        }
    }
}
