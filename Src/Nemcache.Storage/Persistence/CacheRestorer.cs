using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using Nemcache.Storage.IO;
using Nemcache.Storage.Notifications;
using ProtoBuf;

namespace Nemcache.Storage.Persistence
{
    public class CacheRestorer
    {
        private readonly IMemCache _memCache;
        private readonly IFileSystem _fileSystem;
        private readonly string _path;
        private readonly IScheduler _scheduler;

        public CacheRestorer(IMemCache memCache, IFileSystem fileSystem, string path) : 
            this(memCache, fileSystem, path, Scheduler.Default)
        {
            
        }

        public CacheRestorer(IMemCache memCache, IFileSystem fileSystem, string path, IScheduler scheduler)
        {
            _memCache = memCache;
            _fileSystem = fileSystem;
            _path = path;
            _scheduler = scheduler;
        }

        public void RestoreCache()
        {
            if (_fileSystem.File.Exists(_path))
            {
                var newLog = RebuildLog().ToArray();

                var tempFileName = Path.GetTempFileName();

                WriteToFile(tempFileName, newLog);
                ReplacePreviousLogFile(tempFileName);

                var storeNotifications = newLog.Where(e => e.Store != null).Select(e => e.Store);
                foreach (var storeNotification in storeNotifications)
                {
                    _memCache.Add(storeNotification.Key, 
                                  storeNotification.Flags, 
                                  storeNotification.Expiry, 
                                  storeNotification.Data);
                }
            }
        }

        private void ReplacePreviousLogFile(string tempFileName)
        {
            var destinationBackupFileName = _path + ".bak";
            _fileSystem.File.Delete(destinationBackupFileName);
            _fileSystem.File.Replace(tempFileName, _path, destinationBackupFileName, true);
        }

        public static IEnumerable<ArchiveEntry> ReadLog(Stream stream)
        {
            while (stream.Position < stream.Length)
            {
                yield return Serializer.DeserializeWithLengthPrefix<ArchiveEntry>(stream, PrefixStyle.Fixed32);
            }
        }

        private void WriteToFile(string path, IEnumerable<ArchiveEntry> newLog)
        {
            using (var stream = _fileSystem.File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                foreach (var archiveEntry in newLog)
                {
                    Serializer.SerializeWithLengthPrefix(stream, archiveEntry, PrefixStyle.Fixed32);
                }
            }
        }

        private IEnumerable<ArchiveEntry> RebuildLog()
        {
            var seenKeys = new HashSet<string>();

            var log = ReadLogFile().ToArray();

            var updatedExpiry = GetUpdatedExpiries(log);

            var storeAndRemoveEntries = log.Where(e => e.Touch == null);

            foreach (var entry in storeAndRemoveEntries)
            {
                var key = GetKey(entry);

                var added = seenKeys.Add(key);
                if (added)
                {
                    var storeNotification = entry.Store;
                    if (storeNotification != null && 
                        IsExpired(_scheduler, storeNotification, updatedExpiry))
                    {
                        yield return new ArchiveEntry
                            {
                                Store = new StoreNotification
                                    {
                                        Key = key,
                                        Flags = storeNotification.Flags,
                                        Expiry = updatedExpiry.ContainsKey(key) ? updatedExpiry[key] : storeNotification.Expiry,
                                        Data = storeNotification.Data,
                                        Operation = StoreOperation.Add,
                                    }
                            };
                    }
                }
            }
        }

        private IEnumerable<ArchiveEntry> ReadLogFile()
        {
            using (var file = _fileSystem.File.Open(_path, FileMode.Open, FileAccess.Read))
            {
                // ToArray to ensure that it really reads in this using block..
                return ReadLog(file).Reverse().TakeWhile(e => e.Clear == null).ToArray();
            }
        }

        private Dictionary<string, DateTime> GetUpdatedExpiries(IEnumerable<ArchiveEntry> log)
        {
            var updatedExpiry = new Dictionary<string, DateTime>();
            var touchNotifications = log.Where(t => t.Touch != null).Select(t => t.Touch);
            foreach (var entry in touchNotifications)
            {
                if (!updatedExpiry.ContainsKey(entry.Key) && entry.Expiry > _scheduler.Now)
                    updatedExpiry[entry.Key] = entry.Expiry;
            }
            return updatedExpiry;
        }

        private static bool IsExpired(IScheduler scheduler, StoreNotification storeNotification, Dictionary<string, DateTime> updatedExpiry)
        {
            return updatedExpiry.ContainsKey(storeNotification.Key) || storeNotification.Expiry > scheduler.Now;
        }

        private static string GetKey(ArchiveEntry entry)
        {
            if (entry.Store != null)
                return entry.Store.Key;
            if (entry.Remove != null)
                return entry.Remove.Key;
            throw new InvalidOperationException();
        }
    }
}