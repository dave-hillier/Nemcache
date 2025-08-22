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

        [Test]
        public void PersistsAfterCrashSimulation()
        {
            var store1 = new BitcaskStore(_fs, _dir, 1000);
            store1.Put("c", new byte[] {7, 8});

            using (var store2 = new BitcaskStore(_fs, _dir, 1000))
            {
                Assert.IsTrue(store2.TryGet("c", out var value));
                CollectionAssert.AreEqual(new byte[] {7, 8}, value);
            }

            store1.Dispose();
        }

        [Test]
        public void CompactionReducesFileSize()
        {
            using (var store = new BitcaskStore(_fs, _dir, 50))
            {
                for (int i = 0; i < 10; i++)
                {
                    var data = new byte[20];
                    store.Put($"k{i}", data);
                }
                store.Put("k0", new byte[5]);
            }

            var originalSize = Directory.GetFiles(_dir, "data.*").Sum(f => new FileInfo(f).Length);

            var compDir = Path.Combine(_dir, "compact");
            Directory.CreateDirectory(compDir);
            using (var source = new BitcaskStore(_fs, _dir, 50))
            using (var dest = new BitcaskStore(_fs, compDir, 1000))
            {
                foreach (var entry in source.Entries())
                    dest.Put(entry.Key, entry.Value);
            }

            var compactSize = Directory.GetFiles(compDir, "data.*").Sum(f => new FileInfo(f).Length);
            Assert.Less(compactSize, originalSize);
        }

        [Test]
        public void ParallelPutGetDeleteOperations()
        {
            using (var store = new BitcaskStore(_fs, _dir, 1000))
            {
                var gate = new object();
                Parallel.For(0, 100, i =>
                {
                    var key = $"k{i}";
                    lock (gate)
                    {
                        store.Put(key, new byte[] { 1 });
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
            using (var store = new BitcaskStore(_fs, _dir, 1000))
            {
                store.Put("good", new byte[] { 1, 2, 3 });
                store.Put("bad", new byte[] { 4, 5, 6 });
            }

            var file = Directory.GetFiles(_dir, "data.*").OrderBy(f => f).Last();
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Write))
            {
                fs.SetLength(fs.Length - 2);
            }

            using (var store = new BitcaskStore(_fs, _dir, 1000))
            {
                Assert.IsTrue(store.TryGet("good", out var val));
                CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, val);
            }
        }
    }
}
