using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nemcache.Service.IO;

namespace Nemcache.Service.Persistence
{
    /// <summary>
    /// Minimal bitcask style store. Each Put appends a record to a data file and
    /// updates the in-memory index mapping keys to file offsets.
    /// This is a proof of concept only and not production ready.
    /// </summary>
    internal class BitcaskStore : IDisposable
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _directory;
        private readonly long _maxFileSize;
        private FileStream? _currentFile;
        private int _currentFileId;
        private readonly Dictionary<string, (int fileId, long offset, int length)> _index = new();
        public IEnumerable<string> Keys => _index.Keys;

        public BitcaskStore(IFileSystem fileSystem, string directory, long maxFileSize)
        {
            _fileSystem = fileSystem;
            _directory = directory;
            _maxFileSize = maxFileSize;
            Directory.CreateDirectory(directory);
            LoadIndex();
        }

        public void Put(string key, byte[] value)
        {
            EnsureFile();
            var offset = _currentFile!.Position;
            WriteEntry(_currentFile, key, value);
            _index[key] = (_currentFileId, offset, value.Length);
        }

        public bool TryGet(string key, out byte[]? value)
        {
            value = null;
            if (!_index.TryGetValue(key, out var info))
                return false;

            var path = DataFilePath(info.fileId);
            using var stream = _fileSystem.File.Open(path, FileMode.Open, FileAccess.Read) as FileStream;
            stream!.Seek(info.offset, SeekOrigin.Begin);
            value = ReadEntry(stream).value;
            return true;
        }

        public void Delete(string key)
        {
            _index.Remove(key);
            EnsureFile();
            WriteEntry(_currentFile!, key, Array.Empty<byte>()); // tombstone
        }

        public IEnumerable<KeyValuePair<string, byte[]>> Entries()
        {
            foreach (var key in _index.Keys)
            {
                if (TryGet(key, out var val) && val != null && val.Length > 0)
                    yield return new KeyValuePair<string, byte[]>(key, val);
            }
        }

        private void EnsureFile()
        {
            if (_currentFile == null || _currentFile.Length >= _maxFileSize)
            {
                _currentFileId++;
                var path = DataFilePath(_currentFileId);
                _currentFile = _fileSystem.File.Open(path, FileMode.OpenOrCreate, FileAccess.Write) as FileStream;
                _currentFile.Seek(0, SeekOrigin.End);
            }
        }

        private string DataFilePath(int id) => Path.Combine(_directory, $"data.{id}");

        private static void WriteEntry(Stream stream, string key, byte[] value)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            var header = BitConverter.GetBytes(keyBytes.Length).Concat(BitConverter.GetBytes(value.Length)).ToArray();
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
            foreach (var file in Directory.GetFiles(_directory, "data.*").OrderBy(f => f))
            {
                _currentFileId = Math.Max(_currentFileId, ExtractId(file));
                using var stream = _fileSystem.File.Open(file, FileMode.OpenOrCreate, FileAccess.Read) as FileStream;
                while (stream!.Position < stream.Length)
                {
                    var offset = stream.Position;
                    var entry = ReadEntry(stream);
                    _index[entry.key] = (ExtractId(file), offset, entry.value.Length);
                }
            }
        }

        private static int ExtractId(string path)
        {
            var name = Path.GetFileName(path);
            var parts = name.Split('.');
            return int.Parse(parts[1]);
        }

        public void Dispose()
        {
            _currentFile?.Dispose();
        }
    }
}
