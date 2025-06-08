using System.IO;
using Nemcache.Storage.IO;

namespace Nemcache.Tests.IO
{
    public class FakeFileSystem : IFileSystem
    {
        public FakeFileSystem(Stream firstStream, Stream secondStream, Stream thirdStream)
        {
            File = new FakeFile(firstStream, secondStream, thirdStream);
        }

        public IFile File { get; private set; }

    }
}