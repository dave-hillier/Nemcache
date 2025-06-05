using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nemcache.Service.IO;

namespace Nemcache.Service.Persistence
{
    /// <summary>
    /// Simplified hybrid log store inspired by Microsoft's FASTER.
    /// New updates are buffered in memory and periodically flushed to a
    /// single append-only log file on disk. An in-memory index maps keys
    /// to offsets either in the memory buffer or the persisted log.
    /// </summary>
    internal class HybridLogStore : IDisposable
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _logPath;
        private readonly long _memoryLimit;
        private MemoryStream _memLog = new();
        private FileStream? _logFile;

        private struct Entry
        {
            public bool InMemory;
            public long Offset;
            public int Length;
        }

        private readonly Dictionary<string, Entry> _index = new();
        public IEnumerable<string> Keys => _index.Keys;

        public HybridLogStore(IFileSystem fileSystem, string path, long memoryLimit)
        {
            _fileSystem = fileSystem;
            _logPath = path;
            _memoryLimit = memoryLimit;
            var directory = Path.GetDirectoryName(path) ?? ".";
            Directory.CreateDirectory(directory);
            LoadIndex();
        }

        public void Put(string key, byte[] value)
        {
            var offset = _memLog.Position;
            WriteEntry(_memLog, key, value);
            _index[key] = new Entry { InMemory = true, Offset = offset, Length = value.Length };
            if (_memLog.Length >= _memoryLimit)
                Flush();
        }

        public bool TryGet(string key, out byte[]? value)
        {
            value = null;
            if (!_index.TryGetValue(key, out var entry))
                return false;

            if (entry.InMemory)
            {
                _memLog.Position = entry.Offset;
                value = ReadEntry(_memLog).value;
                return true;
            }

            using var stream = _fileSystem.File.Open(_logPath, FileMode.OpenOrCreate, FileAccess.Read) as FileStream;
            stream!.Seek(entry.Offset, SeekOrigin.Begin);
            value = ReadEntry(stream).value;
            return true;
        }

        public void Delete(string key)
        {
            _index.Remove(key);
            WriteEntry(_memLog, key, Array.Empty<byte>()); // tombstone
            if (_memLog.Length >= _memoryLimit)
                Flush();
        }

        public IEnumerable<KeyValuePair<string, byte[]>> Entries()
        {
            foreach (var key in _index.Keys)
                if (TryGet(key, out var val) && val != null && val.Length > 0)
                    yield return new KeyValuePair<string, byte[]>(key, val);
        }

        private void EnsureFile()
        {
            if (_logFile == null)
            {
                _logFile = _fileSystem.File.Open(_logPath, FileMode.OpenOrCreate, FileAccess.Write) as FileStream;
                _logFile.Seek(0, SeekOrigin.End);
            }
        }

        private void Flush()
        {
            if (_memLog.Length == 0)
                return;

            EnsureFile();
            var baseOffset = _logFile!.Length;
            _memLog.Position = 0;
            _memLog.CopyTo(_logFile);
            _logFile.Flush();

            foreach (var key in _index.Keys.ToList())
            {
                var entry = _index[key];
                if (entry.InMemory)
                {
                    entry.InMemory = false;
                    entry.Offset = baseOffset + entry.Offset;
                    _index[key] = entry;
                }
            }

            _memLog.Dispose();
            _memLog = new MemoryStream();
        }

        private static void WriteEntry(Stream stream, string key, byte[] value)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            var header = BitConverter.GetBytes(keyBytes.Length)
                .Concat(BitConverter.GetBytes(value.Length)).ToArray();
            stream.Write(header, 0, header.Length);
            stream.Write(keyBytes, 0, keyBytes.Length);
            stream.Write(value, 0, value.Length);
        }

        private static (string key, byte[] value) ReadEntry(Stream stream)
        {
            Span<byte> header = stackalloc byte[8];
            stream.Read(header);
            var keyLen = BitConverter.ToInt32(header.Slice(0, 4));
            var valLen = BitConverter.ToInt32(header.Slice(4, 4));
            var keyBytes = new byte[keyLen];
            stream.Read(keyBytes, 0, keyLen);
            var valBytes = new byte[valLen];
            stream.Read(valBytes, 0, valLen);
            return (System.Text.Encoding.UTF8.GetString(keyBytes), valBytes);
        }

        private void LoadIndex()
        {
            if (!_fileSystem.File.Exists(_logPath))
                return;

            using var stream = _fileSystem.File.Open(_logPath, FileMode.OpenOrCreate, FileAccess.Read) as FileStream;
            while (stream!.Position < stream.Length)
            {
                var offset = stream.Position;
                var entry = ReadEntry(stream);
                if (entry.value.Length == 0)
                    _index.Remove(entry.key);
                else
                    _index[entry.key] = new Entry { InMemory = false, Offset = offset, Length = entry.value.Length };
            }
        }

        public void Dispose()
        {
            Flush();
            _logFile?.Dispose();
            _memLog.Dispose();
        }
    }
}
