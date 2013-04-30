using System;
using System.IO;

namespace Nemcache.Service.FileSystem
{
    public class PartitioningFileStream : Stream
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _filename;
        private readonly string _ext;
        private readonly int _partitionLength;

        private int _currentPartition = 1;
        private Stream _currentStream;

        public PartitioningFileStream(IFileSystem fileSystem, 
            string filename, string ext, int partitionLength)
        {
            _fileSystem = fileSystem;
            _filename = filename;
            _ext = ext;
            _partitionLength = partitionLength;
            _currentStream = _fileSystem.File.Open(
                GetFileName(_currentPartition), FileMode.Open, FileAccess.Read);
        }

        private string GetFileName(int partition)
        {
            return string.Format("{0}.{1}.{2}", _filename, partition, _ext);
        }

        public override void Flush()
        {
            _currentStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read != count && _currentStream != null)
            {
                read += _currentStream.Read(buffer, read + offset, count - read);
                if (_currentStream.Length == _currentStream.Position)
                {
                    var path = GetFileName(++_currentPartition);
                    if (_fileSystem.File.Exists(path))
                        _currentStream = _fileSystem.File.Open(path, FileMode.Open, FileAccess.Read);
                    else
                        _currentStream = null;
                }
            }
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        public override bool CanRead
        {
            get { return _currentStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
    }
}