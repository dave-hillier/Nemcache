using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;
using Nemcache.Service.FileSystem;

namespace Nemcache.Tests
{
    [TestClass]
    public class PartitioningFileStreamTests
    {

        [TestMethod]
        public void CanRead()
        {
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes("Hello, World!"));
            var fileSystem = new FakeFileSystem(memoryStream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100);

            Assert.IsTrue(partitioningStream.CanRead);
        }

        [TestMethod]
        public void Read()
        {
            const string helloWorld = "Hello, World!";
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(helloWorld));
            var fileSystem = new FakeFileSystem(memoryStream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100);

            var buffer = new byte[helloWorld.Length];
            var read = partitioningStream.Read(buffer, 0, helloWorld.Length);
            Assert.AreEqual(helloWorld.Length, read);
            Assert.AreEqual(helloWorld, Encoding.ASCII.GetString(buffer));
        }

        [TestMethod]
        public void ReadSingleFile()
        {
            const string helloWorld = "Hello, World!!";
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(helloWorld));
            var fileSystem = new FakeFileSystem(memoryStream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100);

            var buffer = new byte[helloWorld.Length];
            for (int i = 0; i < helloWorld.Length; i += 2)
            {
                partitioningStream.Read(buffer, i, 2);
            }
            Assert.AreEqual(helloWorld, Encoding.ASCII.GetString(buffer));
        }

        [TestMethod]
        public void ReadAcrossTwoFiles()
        {
            var memoryStream1 = new MemoryStream(Encoding.ASCII.GetBytes("12345"));
            var memoryStream2 = new MemoryStream(Encoding.ASCII.GetBytes("67890"));
            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 5);

            var buffer = new byte[10];
            var read = partitioningStream.Read(buffer, 0, 10);
            Assert.AreEqual(10, read);
            Assert.AreEqual("1234567890", Encoding.ASCII.GetString(buffer));
        }

        [TestMethod]
        public void ReadAcrossThreeFiles()
        {
            var memoryStream1 = new MemoryStream(Encoding.ASCII.GetBytes("12345"));
            var memoryStream2 = new MemoryStream(Encoding.ASCII.GetBytes("67890"));
            var memoryStream3 = new MemoryStream(Encoding.ASCII.GetBytes("abcde"));
            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, memoryStream3);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 5);

            var buffer = new byte[12];
            var read = partitioningStream.Read(buffer, 0, 12);
            Assert.AreEqual(12, read);
            Assert.AreEqual("1234567890ab", Encoding.ASCII.GetString(buffer));
        }

        class TestStream : Stream
        {
            public bool HasCalledFlush { get; private set; }
            public override void Flush()
            {
                HasCalledFlush = true;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new System.NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new System.NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new System.NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new System.NotImplementedException();
            }

            public override bool CanRead
            {
                get { throw new System.NotImplementedException(); }
            }

            public override bool CanSeek
            {
                get { throw new System.NotImplementedException(); }
            }

            public override bool CanWrite
            {
                get { throw new System.NotImplementedException(); }
            }

            public override long Length
            {
                get { throw new System.NotImplementedException(); }
            }

            public override long Position { get; set; }
        }

        [TestMethod]
        public void FlushWillFlushInnerStream()
        {
            var stream = new TestStream();
            var fileSystem = new FakeFileSystem(stream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100);

            partitioningStream.Flush();

            Assert.IsTrue(stream.HasCalledFlush);
        }

    
    }
}