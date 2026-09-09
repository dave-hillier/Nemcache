using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;

namespace Nemcache.Tests.Persistence
{
    /// <summary>
    /// Bug: BitcaskStore.LoadIndex does not handle tombstones (deleted entries).
    /// When Delete is called, it writes a record with an empty value array as a tombstone.
    /// On reload, LoadIndex reads these tombstones and adds them to the index,
    /// causing deleted keys to reappear.
    /// Compare with HybridLogStore.LoadIndex which correctly removes tombstoned keys.
    /// See BitcaskStore.cs line 121.
    /// </summary>
    [TestFixture]
    public class BitcaskTombstoneLoadIndexBugTest
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

            public bool Exists(string path) => System.IO.File.Exists(path);
            public long Size(string filename) => new FileInfo(filename).Length;
            public void Delete(string path) => System.IO.File.Delete(path);
            public void Replace(string sourceFileName, string destinationFileName,
                string destinationBackupFileName, bool ignoreMetadataErrors)
                => System.IO.File.Replace(sourceFileName, destinationFileName,
                    destinationBackupFileName, ignoreMetadataErrors);
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
        public void DeletedKeysShouldNotReappearAfterReload()
        {
            // Put a key, then delete it, then close
            using (var store = new BitcaskStore(_fs, _dir, 1000))
            {
                store.Put("key1", new byte[] { 1, 2, 3 });
                store.Put("key2", new byte[] { 4, 5, 6 });
                store.Delete("key1");

                // Verify it's deleted in the current session
                Assert.That(store.TryGet("key1", out _), Is.False, "key1 should be deleted in current session");
                Assert.That(store.TryGet("key2", out _), Is.True, "key2 should still exist in current session");
            }

            // Reload - deleted key should NOT reappear
            using (var store2 = new BitcaskStore(_fs, _dir, 1000))
            {
                Assert.That(store2.TryGet("key1", out _), Is.False, "key1 was deleted before restart and should not reappear after reload");
                Assert.That(store2.TryGet("key2", out var val), Is.True, "key2 should persist across restart");
                Assert.That(val, Is.EqualTo(new byte[] { 4, 5, 6 }));
            }
        }

        [Test]
        public void DeletedKeysShouldNotAppearInKeysAfterReload()
        {
            using (var store = new BitcaskStore(_fs, _dir, 1000))
            {
                store.Put("a", new byte[] { 1 });
                store.Put("b", new byte[] { 2 });
                store.Delete("a");
            }

            using (var store2 = new BitcaskStore(_fs, _dir, 1000))
            {
                Assert.That(store2.Keys.ToArray(), Does.Not.Contain("a"), "Deleted key 'a' should not appear in Keys after reload");
                Assert.That(store2.Keys.ToArray(), Does.Contain("b"));
            }
        }
    }
}
