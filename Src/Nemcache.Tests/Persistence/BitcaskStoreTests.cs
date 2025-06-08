using System;
using System.IO;
using NUnit.Framework;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;

namespace Nemcache.Tests.Persistence
{
    [TestFixture]
    public class BitcaskStoreTests
    {
        private string _dir;
        private IFileSystem _fs;

        private class SharedFileSystem : IFileSystem, IFile
        {
            public IFile File => this;

            public Stream Open(string path, FileMode mode, FileAccess access)
            {
                return new FileStream(path, mode, access, FileShare.ReadWrite);
            }

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
            using (var store = new BitcaskStore(_fs, _dir, 1000))
            {
                store.Put("k", new byte[] {1,2,3});
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            using (var store = new BitcaskStore(_fs, _dir, 1000))
            {
                Assert.IsTrue(store.TryGet("k", out var value));
                CollectionAssert.AreEqual(new byte[] {1,2,3}, value);
            }
        }

        [Test]
        public void PersistAndReload()
        {
            using (var store = new BitcaskStore(_fs, _dir, 1000))
            {
                store.Put("a", new byte[] {4,5,6});
            }

            using (var store2 = new BitcaskStore(_fs, _dir, 1000))
            {
                Assert.IsTrue(store2.TryGet("a", out var value));
                CollectionAssert.AreEqual(new byte[] {4,5,6}, value);
            }
        }

        [Test]
        public void DeleteRemovesItem()
        {
            using (var store = new BitcaskStore(_fs, _dir, 1000))
            {
                store.Put("z", new byte[] {9});
                store.Delete("z");
                Assert.IsFalse(store.TryGet("z", out _));
            }
        }
    }
}
