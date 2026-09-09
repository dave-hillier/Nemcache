using System.IO;
using System.Text;
using NUnit.Framework;
using Nemcache.Storage.IO;

namespace Nemcache.Tests.IO
{
    [TestFixture]
    public class PartitioningFileStreamTests
    {
        [Test]
        public void CanRead()
        {
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes("Hello, World!"));
            var fileSystem = new FakeFileSystem(memoryStream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100, FileAccess.Read);

            Assert.That(partitioningStream.CanRead, Is.True);
        }

        [Test]
        public void CanWrite()
        {
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes("Hello, World!"));
            var fileSystem = new FakeFileSystem(memoryStream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100, FileAccess.Write);

            Assert.That(partitioningStream.CanWrite, Is.True);
        }

        [Test]
        public void CanWriteAndCanRead()
        {
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes("Hello, World!"));
            var fileSystem = new FakeFileSystem(memoryStream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100, FileAccess.ReadWrite);

            Assert.That(partitioningStream.CanRead, Is.True);
            Assert.That(partitioningStream.CanWrite, Is.True);
        }

        [Test]
        public void Read()
        {
            const string helloWorld = "Hello, World!";
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(helloWorld));
            var fileSystem = new FakeFileSystem(memoryStream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100, FileAccess.Read);

            var buffer = new byte[helloWorld.Length];
            var read = partitioningStream.Read(buffer, 0, helloWorld.Length);
            Assert.That(read, Is.EqualTo(helloWorld.Length));
            Assert.That(Encoding.ASCII.GetString(buffer), Is.EqualTo(helloWorld));
        }

        [Test]
        public void ReadSingleFile()
        {
            const string helloWorld = "Hello, World!!";
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(helloWorld));
            var fileSystem = new FakeFileSystem(memoryStream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100, FileAccess.Read);

            var buffer = new byte[helloWorld.Length];
            for (int i = 0; i < helloWorld.Length; i += 2)
            {
                partitioningStream.Read(buffer, i, 2);
            }
            Assert.That(Encoding.ASCII.GetString(buffer), Is.EqualTo(helloWorld));
        }

        [Test]
        public void ReadAcrossTwoFiles()
        {
            var memoryStream1 = new MemoryStream(Encoding.ASCII.GetBytes("12345"));
            var memoryStream2 = new MemoryStream(Encoding.ASCII.GetBytes("67890"));
            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 5, FileAccess.Read);

            var buffer = new byte[10];
            var read = partitioningStream.Read(buffer, 0, 10);
            Assert.That(read, Is.EqualTo(10));
            Assert.That(Encoding.ASCII.GetString(buffer), Is.EqualTo("1234567890"));
        }

        [Test]
        public void ReadAcrossThreeFiles()
        {
            var memoryStream1 = new MemoryStream(Encoding.ASCII.GetBytes("12345"));
            var memoryStream2 = new MemoryStream(Encoding.ASCII.GetBytes("67890"));
            var memoryStream3 = new MemoryStream(Encoding.ASCII.GetBytes("abcde"));
            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, memoryStream3);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 5, FileAccess.Read);

            var buffer = new byte[12];
            var read = partitioningStream.Read(buffer, 0, 12);
            Assert.That(read, Is.EqualTo(12));
            Assert.That(Encoding.ASCII.GetString(buffer), Is.EqualTo("1234567890ab"));
        }

        [Test]
        public void FlushWillFlushInnerStream()
        {
            var stream = new TestStream();
            var fileSystem = new FakeFileSystem(stream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100, FileAccess.Read);

            partitioningStream.Flush();

            Assert.That(stream.HasCalledFlush, Is.True);
        }

        [Test]
        public void DisposeWillCloseInnerStream()
        {
            var stream = new TestStream();
            var fileSystem = new FakeFileSystem(stream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100, FileAccess.Read);

            partitioningStream.Dispose();

            Assert.That(stream.HasCalledClose, Is.True);
        }

        // TODO: check the filename for read

        [Test]
        public void Write()
        {
            var memoryStream = new MemoryStream();
            var fileSystem = new FakeFileSystem(memoryStream, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 100, FileAccess.Write);

            const string helloWorld = "Hello, World!";

            var buffer = Encoding.ASCII.GetBytes(helloWorld);
            partitioningStream.Write(buffer, 0, buffer.Length);
            partitioningStream.Flush();

            Assert.That(Encoding.ASCII.GetString(memoryStream.ToArray()), Is.EqualTo(helloWorld));
        }

        [Test]
        public void WriteTwoFiles()
        {
            var memoryStream1 = new MemoryStream();
            var memoryStream2 = new MemoryStream();
            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 6, FileAccess.Write);

            const string helloWorld = "Hello, World!";

            var buffer = Encoding.ASCII.GetBytes(helloWorld);
            partitioningStream.Write(buffer, 0, buffer.Length);
            partitioningStream.Flush();

            Assert.That(Encoding.ASCII.GetString(memoryStream1.ToArray()), Is.EqualTo(helloWorld.Substring(0, 6)));
            Assert.That(Encoding.ASCII.GetString(memoryStream2.ToArray()), Is.EqualTo(helloWorld.Substring(6, 6)));
        }

        // TODO: Read then Write


        // TODO: position (changes on read or write), length (only changes on write)

        [Test]
        public void LengthZero()
        {
            var fileSystem = new FakeFileSystem(null, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 6, FileAccess.Write);

            Assert.That(partitioningStream.Length, Is.EqualTo(0));
        }

        [Test]
        public void Length()
        {
            var memoryStream1 = new MemoryStream(new byte[10]);
            var memoryStream2 = new MemoryStream(new byte[10]);

            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 6, FileAccess.Write);

            Assert.That(partitioningStream.Length, Is.EqualTo(20));
        }

        [Test]
        public void ExpandLengthOneFile()
        {
            var memoryStream1 = new MemoryStream();
            var memoryStream2 = new MemoryStream();

            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 10, FileAccess.Write);
            partitioningStream.Write(new byte[20], 0, 5);
            Assert.That(partitioningStream.Length, Is.EqualTo(5));
        }

        [Test]
        public void ExpandLengthTwoFiles()
        {
            var memoryStream1 = new MemoryStream();
            var memoryStream2 = new MemoryStream();

            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 10, FileAccess.Write);
            partitioningStream.Write(new byte[20], 0, 15);
            Assert.That(partitioningStream.Length, Is.EqualTo(15));
        }

        [Test]
        public void StartPosition()
        {
            var fileSystem = new FakeFileSystem(null, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 6, FileAccess.Write);

            Assert.That(partitioningStream.Position, Is.EqualTo(0));
        }

        [Test]
        public void PositionAfterRead()
        {
            var memoryStream1 = new MemoryStream(new byte[10]);

            var fileSystem = new FakeFileSystem(memoryStream1, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 6, FileAccess.Write);
            var buffer = new byte[9];
            partitioningStream.Read(buffer, 0, 9);
            Assert.That(partitioningStream.Position, Is.EqualTo(9));
        }

        [Test]
        public void PositionAfterWrite()
        {
            var memoryStream1 = new MemoryStream();

            var fileSystem = new FakeFileSystem(memoryStream1, null, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 10, FileAccess.Write);

            var buffer = new byte[9];
            partitioningStream.Write(buffer, 0, 9);
            Assert.That(partitioningStream.Position, Is.EqualTo(9));
        }

        [Test]
        public void PositionAfterReadThenWrite()
        {
            var memoryStream1 = new MemoryStream(new byte[20]);
            var memoryStream2 = new MemoryStream();

            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 20, FileAccess.Write);

            var buffer = new byte[9];
            partitioningStream.Read(buffer, 0, 4);
            partitioningStream.Write(buffer, 0, 9);
            Assert.That(partitioningStream.Position, Is.EqualTo(13));
        }

        [Test]
        public void PositionAfterReadThenWriteIntoSecondFile()
        {
            var memoryStream1 = new MemoryStream(new byte[20]);
            var memoryStream2 = new MemoryStream(new byte[20]);

            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 20, FileAccess.Write);

            var buffer = new byte[40];
            partitioningStream.Read(buffer, 0, 20);
            partitioningStream.Write(buffer, 0, 10);
            Assert.That(partitioningStream.Position, Is.EqualTo(30));
        }

        [Test]
        public void OverwriteDoesntChangeLength()
        {
            var memoryStream1 = new MemoryStream(new byte[10]);
            var memoryStream2 = new MemoryStream(new byte[10]);

            var fileSystem = new FakeFileSystem(memoryStream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 6, FileAccess.Write);
            partitioningStream.Write(new byte[5], 0, 5);
            Assert.That(partitioningStream.Length, Is.EqualTo(20));
        }

        [Test]
        public void FilesAreClosed()
        {
            var stream1 = new TestStream();
            var memoryStream2 = new MemoryStream();
            var fileSystem = new FakeFileSystem(stream1, memoryStream2, null);
            var partitioningStream = new PartitioningFileStream(fileSystem, "FileName", "ext", 6, FileAccess.Write);

            const string helloWorld = "Hello, World!";

            var buffer = Encoding.ASCII.GetBytes(helloWorld);
            partitioningStream.Write(buffer, 0, buffer.Length);

            Assert.That(stream1.HasCalledClose, Is.True);
        }
    }
}