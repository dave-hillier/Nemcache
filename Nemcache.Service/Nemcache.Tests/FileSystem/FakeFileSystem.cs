using System.IO;
using Nemcache.Service.FileSystem;

namespace Nemcache.Tests.FileSystem
{
    public class FakeFileSystem : IFileSystem
    {
        public FakeFileSystem(Stream firstStream, Stream secondStream, Stream thirdStream)
        {
            File = new FakeFile(firstStream, secondStream, thirdStream);
        }

        public IFile File { get; private set; }

        public class FakeFile : IFile
        {
            private readonly Stream _stream1;
            private readonly Stream _stream2;
            private readonly Stream _stream3;

            public FakeFile(Stream stream1, Stream stream2, Stream stream3)
            {
                _stream1 = stream1;
                _stream2 = stream2;
                _stream3 = stream3;
            }

            public Stream Open(string path, FileMode mode, FileAccess access)
            {
                if (path.Contains(".1."))
                    return _stream1;
                if (path.Contains(".2."))
                    return _stream2;
                return _stream3;
            }

            public bool Exists(string path)
            {
                return Open(path, FileMode.Open, FileAccess.Read) != null;
            }
        }
    }
}