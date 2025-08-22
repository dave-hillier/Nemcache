using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            public bool Exists(string path) => System.IO.File.Exists(path);
            public long Size(string filename) => new FileInfo(filename).Length;
            public void Delete(string path) => System.IO.File.Delete(path);
            public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
                => System.IO.File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
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

        [Test]
        public void CompactionReducesLogFileSize()
        {
            var path = Path.Combine(_dir, "log.dat");
            using (var store = new HybridLogStore(_fs, path, 20))
            {
                store.Put("a", new byte[] {1,2,3,4,5});
                store.Put("a", new byte[] {6});
                store.Put("b", new byte[] {7,8,9});
                store.Delete("b");
            }

            var originalSize = new FileInfo(path).Length;
            var compactPath = Path.Combine(_dir, "compact.dat");
            using (var source = new HybridLogStore(_fs, path, 20))
            using (var dest = new HybridLogStore(_fs, compactPath, 1024))
            {
                foreach (var entry in source.Entries())
                    dest.Put(entry.Key, entry.Value);
            }

            var compactSize = new FileInfo(compactPath).Length;
            Assert.Less(compactSize, originalSize);
        }

        [Test]
        public void ParallelPutGetDeleteOperations()
        {
            var path = Path.Combine(_dir, "log.dat");
            using (var store = new HybridLogStore(_fs, path, 100))
            {
                var gate = new object();
                Parallel.For(0, 100, i =>
                {
                    var key = $"k{i}";
                    lock (gate)
                    {
                        store.Put(key, new byte[] {1});
                        Assert.IsTrue(store.TryGet(key, out _));
                        store.Delete(key);
                    }
                });

                Assert.IsEmpty(store.Keys);
            }
        }

        [Test]
        public void RecoversFromTruncatedLog()
        {
            var path = Path.Combine(_dir, "log.dat");
            using (var store = new HybridLogStore(_fs, path, 100))
            {
                store.Put("good", new byte[] {1,2,3});
                store.Put("bad", new byte[] {4,5,6});
            }

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Write))
            {
                fs.SetLength(fs.Length - 2);
            }

            using (var store = new HybridLogStore(_fs, path, 100))
            {
                Assert.IsTrue(store.TryGet("good", out var val));
                CollectionAssert.AreEqual(new byte[] {1,2,3}, val);
            }
        }
    }
}
