using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nemcache.Service.IO;
using Nemcache.Service.Notifications;
using ProtoBuf;

namespace Nemcache.Service.Persistence
{
    internal class StreamArchiver : IObserver<ICacheNotification> 
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _cacheArchivePath;
        private readonly MemCache _memCache;
        private Stream _outputStream;
        private int _lastVersion;


        public StreamArchiver(IFileSystem fileSystem, string cacheArchivePath, MemCache memCache, long compactThreshold)
        {
            _fileSystem = fileSystem;
            _cacheArchivePath = cacheArchivePath;
            _memCache = memCache;
            _outputStream = fileSystem.File.Open(cacheArchivePath, FileMode.OpenOrCreate, FileAccess.Write);
            CompactThreshold = compactThreshold;
        }

        public void OnNext(ICacheNotification value)
        {
            if (value is RetrieveNotification)
                return;

            var entry = CreateArchiveEntry(value);
            OnNotification(value.EventId, entry);
        }

        public void OnError(Exception error)
        {
            // Cache emits an error?
            _outputStream.Dispose();
        }

        public void OnCompleted()
        {
            // Dispose?
            _outputStream.Dispose();
        }

        private void OnNotification(int version, ArchiveEntry archiveEntry)
        {
            if (_lastVersion > version)
                return;

            if (!Full)
            {
                WriteSingleNotification(archiveEntry);
                _lastVersion = version;
            }
            else
            {
                CompactCache();
            }
        }

        private void CompactCache()
        {
            var path = Path.GetTempFileName();
            using (var newCache = _fileSystem.File.Open(path, FileMode.CreateNew, FileAccess.Write))
            {
                var currentState = _memCache.CurrentState;
                var archiveEntries = currentState.Item2.Select(e => CreateArchiveEntry(e.Key, e.Value));
                WriteToFile(newCache, archiveEntries);
                _lastVersion = currentState.Item1;
            }
            var destinationBackupFileName = _cacheArchivePath + ".bak";
            _fileSystem.File.Delete(destinationBackupFileName);
            _fileSystem.File.Replace(path, _cacheArchivePath, destinationBackupFileName, true);
            _outputStream = _fileSystem.File.Open(_cacheArchivePath, FileMode.OpenOrCreate, FileAccess.Write);
        }

        public ArchiveEntry CreateArchiveEntry(string key, CacheEntry entry)
        {
            return new ArchiveEntry
                {
                    Store = new StoreNotification
                        {
                            Data = entry.Data,
                            EventId = 0,
                            Expiry = entry.Expiry,
                            Flags = entry.Flags,
                            IsSnapshot = true, // TODO: remove this field?
                            Key = key,
                            Operation = StoreOperation.Add
                        }
                };
        }

        private void WriteToFile(Stream stream, IEnumerable<ArchiveEntry> newLog)
        {
            foreach (var archiveEntry in newLog)
            {
                Serializer.SerializeWithLengthPrefix(stream, archiveEntry, PrefixStyle.Fixed32);
            }
        }

        private void WriteSingleNotification(ArchiveEntry archiveEntry)
        {
            Serializer.SerializeWithLengthPrefix(_outputStream, archiveEntry, PrefixStyle.Fixed32);
            _outputStream.FlushAsync();
        }
        private long CompactThreshold { get; set; }
        private bool Full
        {
            get { return _outputStream.Length > CompactThreshold; }
        }

        private static ArchiveEntry CreateArchiveEntry(ICacheNotification notification)
        {
            var archiveEntry = new ArchiveEntry
                {
                    Store = notification as StoreNotification,
                    Clear = notification as ClearNotification,
                    Remove = notification as RemoveNotification,
                    Touch = notification as TouchNotification
                };
            return archiveEntry;
        }
    }
}