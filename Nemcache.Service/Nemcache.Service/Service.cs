using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nemcache.Service.FileSystem;
using Nemcache.Service.Notifications;

namespace Nemcache.Service
{
    class CompactingFileArchiverService
    {
        private readonly FileSystemWrapper _fileSystem;
        private readonly uint _partitionSize;
        private StreamArchiver _archiver;

        public CompactingFileArchiverService(FileSystemWrapper fileSystem, uint partitionSize)
        {
            _fileSystem = fileSystem;
            _partitionSize = partitionSize;
        }

        public void Dispose()
        {
            
        }

        public IMemCache CreateFromLog(string fileNameWithoutExtension, string extension)
        {
            IMemCache memCache = null;
            RestoreFromLog(memCache, fileNameWithoutExtension, extension);
            return memCache;
        }

        public void RestoreFromLog(IMemCache memCache, string fileNameWithoutExtension, string extension)
        {
            if (_fileSystem.File.Exists(fileNameWithoutExtension + ".0." + extension))
            {
                using (var existingLog = new PartitioningFileStream(
                    _fileSystem, fileNameWithoutExtension, extension, _partitionSize, FileAccess.Read))
                {
                    StreamArchiver.Restore(existingLog, memCache);
                }
            }
        }

        public PartitioningFileStream CleanupAndCreateNew(uint partitionLength, string fileNameWithoutExtension, string extension)
        {
            var generator = new LogFileNameGenerator(fileNameWithoutExtension, extension);
            var existingFiles = generator.GetNextFileName().TakeWhile(fn => _fileSystem.File.Exists(fn)); // TODO: repetition

            // TODO: should avoid deleting before we have backed up the current state. This means will probably need to know when we're logging live state.
            foreach (var existingFile in existingFiles)
            {
                _fileSystem.File.Delete(existingFile);
            }

            return new PartitioningFileStream(_fileSystem, fileNameWithoutExtension, extension, partitionLength,
                                              FileAccess.Read);
        }

        public IObservable<Unit> CreateCompactNotifications(IObservable<int> writeNotifications, uint threshold)
        {
            return writeNotifications.
                Scan(0, (acc, length) => acc + length).
                TakeWhile(acc => acc > threshold).
                Repeat().
                Throttle(TimeSpan.FromMinutes(1)). // TODO: configure
                Select(_ => new Unit());
        }
                
        /*private void DoCompact()
        {
            if (_archiver != null)
                _archiver.Dispose();

            var newLog = CleanupAndCreateNew(_partitionSize, LogFileNameWithoutExtension, LogFileExtension);
            _archiver = new StreamArchiver(newLog, _memCache.Notifications);
        }*/

    }

    internal class Service
    {
        private readonly RequestResponseTcpServer _server;
        private readonly MemCache _memCache;
        private readonly string _cacheFileName;
        private readonly uint _partitionSize;
        private readonly FileSystemWrapper _fileSystem;
        private readonly CompactingFileArchiverService _compactingFileArchiverService;

        public Service(ulong capacity, uint port, string cacheFileName, uint partitionSize)
        {
            _partitionSize = partitionSize;
            _cacheFileName = cacheFileName;
            _memCache = new MemCache(capacity);
            _compactingFileArchiverService = new CompactingFileArchiverService(_fileSystem, partitionSize);

            var requestHandler = new RequestHandler(Scheduler.Default, _memCache);
            _server = new RequestResponseTcpServer(IPAddress.Any, port, requestHandler.Dispatch);
            _fileSystem = new FileSystemWrapper();
        }

        private string LogFileExtension
        {
            get { return Path.GetExtension(_cacheFileName); }
        }

        private string LogFileNameWithoutExtension
        {
            get { return Path.GetFileNameWithoutExtension(_cacheFileName); }
        }

        public void Start()
        {
            var fileNameWithoutExtension = LogFileNameWithoutExtension;
            var extension = LogFileExtension;

            _compactingFileArchiverService.RestoreFromLog(_memCache, fileNameWithoutExtension, extension);

            var newLog = _compactingFileArchiverService.CleanupAndCreateNew(_partitionSize, fileNameWithoutExtension, extension);

            // Subscribing after restore has the effect of compacting the cache.
            var notifications = _memCache.Notifications;
            //_archiver = new StreamArchiver(newLog, notifications);

            //SetupCompact(notifications);

            _server.Start();
        }

        private static IObservable<int> WriteNotifications(IObservable<ICacheNotification> notifications)
        {
            return notifications.OfType<StoreNotification>().Select(n => n.Data.Length);
        }

        public void Stop()
        {
            _server.Stop();
            //_archiver.Dispose();
            _compactingFileArchiverService.Dispose();
        }
    }
}