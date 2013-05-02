using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nemcache.Service.FileSystem;
using Nemcache.Service.Notifications;

namespace Nemcache.Service
{
    internal class Service
    {
        private readonly RequestResponseTcpServer _server;
        private readonly MemCache _memCache;
        private readonly string _cacheFileName;
        private readonly uint _partitionSize;
        private readonly FileSystemWrapper _fileSystem;
        private StreamArchiver _archiver;

        public Service(ulong capacity, uint port, string cacheFileName, uint partitionSize)
        {
            _partitionSize = partitionSize;
            _cacheFileName = cacheFileName;
            _memCache = new MemCache(capacity);

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

            RestoreFromLog(_memCache, fileNameWithoutExtension, extension);

            CleanUpOldLog(fileNameWithoutExtension, extension);

            _archiver = CreateArchiver();

            // TODO: not entirely happy with this api- its a bit inconsistent. do I use extensions or Factory or normal methods.
            var writeThresholdNotification = new WriteThresholdNotification(_partitionSize*10, TimeSpan.FromHours(1));
            var logWriteNotifications = WriteNotifications(_memCache.Notifications);
            writeThresholdNotification.Create(logWriteNotifications, Scheduler.Default).Subscribe(_=> DoCompact());
            
            _server.Start();
        }

        private void DoCompact()
        {
            CleanUpOldLog(LogFileNameWithoutExtension, LogFileExtension); // TODO: ideally do this after the new archiver has been created/populated.
            DisposeCurrentArchiver();
            _archiver = CreateArchiver();
        }

        private StreamArchiver CreateArchiver()
        {
            var newLog = new PartitioningFileStream(
                _fileSystem, 
                LogFileNameWithoutExtension, LogFileExtension, 
                _partitionSize,
                FileAccess.Write);

            return new StreamArchiver(newLog, _memCache.Notifications);
        }

        private void DisposeCurrentArchiver()
        {
            if (_archiver != null)
            {
                _archiver.Dispose();
            }
        }

        private static IObservable<long> WriteNotifications(IObservable<ICacheNotification> notifications)
        {
            return notifications.OfType<StoreNotification>().Select(n => (long)n.Data.Length);
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

        public void CleanUpOldLog(string fileNameWithoutExtension, string extension)
        {
            var generator = new LogFileNameGenerator(fileNameWithoutExtension, extension);
            var existingFiles = generator.GetNextFileName().TakeWhile(fn => _fileSystem.File.Exists(fn)); // TODO: repetition

            // TODO: should avoid deleting before we have backed up the current state. This means will probably need to know when we're logging live state.
            foreach (var existingFile in existingFiles)
            {
                _fileSystem.File.Delete(existingFile);
            }
        }


        public void Stop()
        {
            _server.Stop();
            _archiver.Dispose();
        }
    }
}