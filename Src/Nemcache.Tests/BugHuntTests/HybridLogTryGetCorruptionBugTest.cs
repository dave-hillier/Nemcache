using System;
using System.IO;
using NUnit.Framework;
using Nemcache.Storage.IO;
using Nemcache.Storage.Persistence;

namespace Nemcache.Tests.Persistence
{
    /// <summary>
    /// Bug: HybridLogStore.TryGet corrupts the MemoryStream write position.
    /// When reading an in-memory entry, TryGet sets _memLog.Position to the entry's
    /// offset but never restores it to the end. The next Put uses _memLog.Position
    /// as the offset for the new write, overwriting existing data.
    /// See HybridLogStore.cs lines 59-62.
    /// </summary>
    [TestFixture]
    public class HybridLogTryGetCorruptionBugTest
    {
        private string _dir;
        private IFileSystem _fs;

        private class SharedFileSystem : IFileSystem, IFile
        {
            public IFile File => this;
            public Stream Open(string path, FileMode mode, FileAccess access)
                => new FileStream(path, mode, access, FileShare.ReadWrite);
            public bool Exists(string path) => System.IO.File.Exists(path);
            public long Size(string filename) => new FileInfo(filename).Length;
            public void Delete(string path) => System.IO.File.Delete(path);
            public void Replace(string src, string dest, string backup, bool ignore)
                => System.IO.File.Replace(src, dest, backup, ignore);
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
        public void TryGetShouldNotCorruptSubsequentPut()
        {
            var path = Path.Combine(_dir, "log.dat");

            // Use a large memory limit so nothing flushes to disk
            using var store = new HybridLogStore(_fs, path, 10000);

            // Put two entries in memory
            store.Put("A", new byte[] { 1, 2, 3 });
            store.Put("B", new byte[] { 4, 5, 6 });

            // TryGet "A" - this moves the memLog position backwards
            Assert.That(store.TryGet("A", out var valA), Is.True);
            Assert.That(valA, Is.EqualTo(new byte[] { 1, 2, 3 }));

            // Now put a third entry - this should NOT overwrite B
            store.Put("C", new byte[] { 7, 8, 9 });

            // B should still be intact
            Assert.That(store.TryGet("B", out var valB), Is.True, "Key B should still exist after Put(C)");
            Assert.That(valB, Is.EqualTo(new byte[] { 4, 5, 6 }), "Key B's data should not be corrupted by Put(C) after TryGet(A)");

            // C should also be correct
            Assert.That(store.TryGet("C", out var valC), Is.True);
            Assert.That(valC, Is.EqualTo(new byte[] { 7, 8, 9 }));
        }
    }
}
