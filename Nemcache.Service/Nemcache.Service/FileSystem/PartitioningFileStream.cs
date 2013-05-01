using System;
using System.IO;

namespace Nemcache.Service.FileSystem
{
    // TODO: is partitioning a good name?
    public class PartitioningFileStream : Stream
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _filename;
        private readonly string _ext;
        private readonly int _partitionLength;
        private readonly FileAccess _fileAccess;

        private int _currentPartition = 0;
        private Stream _currentStream;
        private long _length;
        private long _position = 0;


        public PartitioningFileStream(IFileSystem fileSystem, 
            string filename, string ext, int partitionLength, FileAccess fileAccess)
        {
            _fileSystem = fileSystem;
            _filename = filename;
            _ext = ext;
            _partitionLength = partitionLength;
            _fileAccess = fileAccess;

            AdvanceToNextFile();
            CalculateLength();
        }

        private void CalculateLength()
        {
            _length = 0;
            int i = 1;
            var filename = GetFileName(i);
            while (_fileSystem.File.Exists(filename))
            {
                _length += _fileSystem.File.Size(filename);
                filename = GetFileName(++i);
            }
        }

        private string GetFileName(int partition)
        {
            return string.Format("{0}.{1}.{2}", _filename, partition, _ext);
        }

        public override void Flush()
        {
            if (_currentStream != null)
                _currentStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read != count && _currentStream != null)
            {
                read += _currentStream.Read(buffer, read + offset, count - read);
                if (_currentStream.Length == _currentStream.Position)
                {
                    AdvanceToNextFile();
                }
            }
            _position += read;
            return read;
        }

        private void AdvanceToNextFile()
        {
            if (_currentStream != null)
                _currentStream.Close();
            var path = GetFileName(++_currentPartition);
            _currentStream = _fileSystem.File.Exists(path)
                                 ? _fileSystem.File.Open(path, FileMode.OpenOrCreate, _fileAccess)
                                 : null;

        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int written = 0;
            while (written < count && _currentStream != null)
            {
                int space = (int)(_partitionLength - _currentStream.Position);
                int remaining = count - written;
                if (remaining > space)
                {
                    _currentStream.Write(buffer, offset+written, space);
                    written += space;
                    AdvanceToNextFile();                    
                }
                else
                {
                    _currentStream.Write(buffer, offset+written, remaining);
                    written += remaining;
                }
            }
            if (written + (int) _position > (int) _length)
            {
                _length += written;
            }
            _position += written;
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
            get { return _currentStream.CanWrite; }
        }

        public override long Length { get { return _length; } }

        public override long Position
        {
            get { return _position; }

            set
            {
                throw new NotImplementedException();
            }
        }
        protected override void Dispose(bool disposing)
        {
            _currentStream.Dispose();
            base.Dispose(disposing);
        }
    }
}