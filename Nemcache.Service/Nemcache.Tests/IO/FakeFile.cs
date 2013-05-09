using System.Collections.Generic;
using System.IO;
using Nemcache.Service.IO;

namespace Nemcache.Tests.IO
{
    public class FakeFile : IFile
    {
        private readonly List<Stream> _streams = new List<Stream>();

        public FakeFile(params Stream[] streams)
        {
            _streams.AddRange(streams);
        }

        public Stream Open(string path, FileMode mode, FileAccess access)
        {
            if (path.Contains("log"))
                return _streams[0];
            if (path.Contains(".1."))
                return _streams[0];
            if (path.Contains(".2."))
                return _streams[1];
            if (path.Contains(".3."))
                return _streams[2];
            return null;
        }

        public bool Exists(string path)
        {
            return Open(path, FileMode.Open, FileAccess.Read) != null;
        }

        public long Size(string path)
        {
            return Open(path, FileMode.Open, FileAccess.Read).Length;
        }

        public void Delete(string path)
        {
        }

        public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName,
                            bool ignoreMetadataErrors)
        {
        }
    }
}