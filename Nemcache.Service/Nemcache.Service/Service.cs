using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nemcache.Service.IO;
using Nemcache.Service.Notifications;
using Nemcache.Service.Reactive;

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
        private IDisposable _archiverSubscription;
        private bool _cleanUpDue;
        private IDisposable _cleanUpSubscription;

        public Service(ulong capacity, uint port, string cacheFileName, uint partitionSize)
        {
            _partitionSize = partitionSize;
            _cacheFileName = cacheFileName;
            _memCache = new MemCache(capacity);

            var requestHandler = new RequestHandler(Scheduler.Default, _memCache);
            _server = new RequestResponseTcpServer(IPAddress.Any, port, requestHandler.Dispatch);
            _fileSystem = new FileSystemWrapper();

            // TODO: not entirely happy with this api- its a bit inconsistent. do I use extensions or Factory or normal methods.
            var writeThresholdNotification = new WriteThresholdNotification(
                _partitionSize * 10, TimeSpan.FromMinutes(1), Scheduler.Default);

            var notifications = _memCache.Notifications.Publish().RefCount();

            var logWriteNotifications = WriteNotifications(notifications);
            var snapshotCompleted = SnapshotCompleted(notifications);

            _cleanUpSubscription = snapshotCompleted.Where(_ => _cleanUpDue).Subscribe(_ => CleanUpOldLog());
            writeThresholdNotification.Create(logWriteNotifications).
                Subscribe(_ => DoCompact());
        }

        private string LogFileNameExtension
        {
            get { return Path.GetExtension(_cacheFileName); }
        }

        private string LogFileNameWithoutExtension
        {
            get { return Path.GetFileNameWithoutExtension(_cacheFileName); }
        }

        public void Start()
        {
            RestoreFromLog();

            _archiver = CreateArchiver();

            _archiverSubscription = _memCache.Notifications.Subscribe(_archiver);

            _server.Start();
        }

        private void DoCompact()
        {
            DisposeCurrentArchiver();
            _archiver = CreateArchiver();
            _archiverSubscription = _memCache.Notifications.Subscribe(_archiver);
        }

        private StreamArchiver CreateArchiver()
        {
            _cleanUpDue = true;
            var newLog = new PartitioningFileStream(
                _fileSystem, 
                LogFileNameWithoutExtension, LogFileNameExtension, 
                _partitionSize,
                FileAccess.Write);

            return new StreamArchiver(newLog);
        }

        private void DisposeCurrentArchiver()
        {
            if (_archiver != null)
            {
                _archiverSubscription.Dispose();
                _archiver.Dispose();
            }
        }

        private static IObservable<long> WriteNotifications(IObservable<ICacheNotification> notifications)
        {
            return notifications.
                OfType<StoreNotification>().
                SkipWhile(n => n.IsSnapshot).
                Select(n => (long)n.Data.Length);
        }

        private static IObservable<Unit> SnapshotCompleted(IObservable<ICacheNotification> notifications)
        {
            return notifications.
                OfType<StoreNotification>().
                SkipWhile(n => n.IsSnapshot).
                Take(1).
                Select(_ => new Unit());
        }

        public void RestoreFromLog()
        {
            if (_fileSystem.File.Exists(LogFileNameWithoutExtension + ".0." + LogFileNameExtension))
            {
                using (var existingLog = new PartitioningFileStream(
                    _fileSystem, LogFileNameWithoutExtension, LogFileNameExtension,
                    _partitionSize, FileAccess.Read))
                {
                    StreamArchiver.Restore(existingLog, _memCache);
                }
            }
        }

        // TODO: separate file
        public void CleanUpOldLog()
        {
            var generator = new LogFileNameGenerator(LogFileNameWithoutExtension, LogFileNameExtension);
            var existingFiles = generator.GetNextFileName().TakeWhile(fn => _fileSystem.File.Exists(fn)); // TODO: repetition

            foreach (var existingFile in existingFiles)
            {
                _fileSystem.File.Delete(existingFile);
            }
            _cleanUpDue = false;
        }


        public void Stop()
        {
            _server.Stop();
            DisposeCurrentArchiver();
        }
    }
}