using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nemcache.Service.IO
{
    // TODO: is partitioning a good name?
    public class PartitioningFileStream : Stream
    {
        private readonly IFileSystem _fileSystem;
        private readonly uint _partitionLength;
        private readonly FileAccess _fileAccess;
        private Stream _currentStream;
        private long _length;
        private long _position = 0;
        private readonly LogFileNameGenerator _logFileNameGenerator;
        private readonly IEnumerator<string> _enumerator;

        public PartitioningFileStream(IFileSystem fileSystem, 
            string filename, string ext, uint partitionLength, FileAccess fileAccess)
        {
            _fileSystem = fileSystem;
            _logFileNameGenerator = new LogFileNameGenerator(filename, ext);
            _enumerator = _logFileNameGenerator.GetNextFileName().GetEnumerator();
            _partitionLength = partitionLength;
            _fileAccess = fileAccess;

            AdvanceToNextFile();
            CalculateLength();
        }

        private void CalculateLength()
        {
            _length = ExistingLogFiles.Sum(fn => _fileSystem.File.Size(fn));
        }

        private IEnumerable<string> ExistingLogFiles
        {
            get 
            { 
                return _logFileNameGenerator.
                    GetNextFileName().
                    TakeWhile(fn => _fileSystem.File.Exists(fn)); 
            }
        }

        public override void Flush()
        {
            if (_currentStream != null)
                _currentStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read != count && _currentStream != null)
            {
                read += _currentStream.Read(buffer, read + offset, count - read);
                if (EndOfCurrentStream)
                {
                    AdvanceToNextFile();
                }
            }
            _position += read;
            return read;
        }

        private bool EndOfCurrentStream
        {
            get { return _currentStream.Length == _currentStream.Position; }
        }

        private void AdvanceToNextFile()
        {
            CloseCurrentStream();
            var path = GetNextFileName();
            _currentStream = _fileSystem.File.Open(path, FileMode.OpenOrCreate, _fileAccess);

        }

        private void CloseCurrentStream()
        {
            if (_currentStream != null)
                _currentStream.Close();
        }

        private string GetNextFileName()
        {
            _enumerator.MoveNext();
            return _enumerator.Current;
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
            if (written + _position >  _length)
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
            get { return false; }
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
                throw new InvalidOperationException();
            }
        }
        protected override void Dispose(bool disposing)
        {
            CloseCurrentStream(); 
            base.Dispose(disposing);
        }
    }
}