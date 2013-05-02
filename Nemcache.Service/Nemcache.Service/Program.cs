using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nemcache.Service.FileSystem;
using Nemcache.Service.Notifications;
using Topshelf;

namespace Nemcache.Service
{
    internal class Program
    {
        private static void Main()
        {
            var capacitySetting = ConfigurationManager.AppSettings["Capacity"];
            ulong capacity = capacitySetting != null ? ulong.Parse(capacitySetting) : 1024 * 1024 * 1024 * 4L; // 4GB

            var portSetting = ConfigurationManager.AppSettings["Port"];
            uint port = portSetting != null ? uint.Parse(portSetting) : 11222;

            var cacheFileName = ConfigurationManager.AppSettings["CacheFile"] ?? "cache.bin";

            var partitionSizeSetting = ConfigurationManager.AppSettings["Port"];
            uint partitionSize = partitionSizeSetting != null ? uint.Parse(partitionSizeSetting) : 512 * 1024 * 1024;

            HostFactory.Run(hc =>
                {
                    hc.Service<Service>(s =>
                        {
                            s.ConstructUsing(() => new Service(capacity, port, cacheFileName, partitionSize));
                            s.WhenStarted(xs => xs.Start());
                            s.WhenStopped(xs => xs.Stop());
                        });
                    hc.RunAsNetworkService();
                    hc.SetDescription("Simple .NET implementation of Memcache; an in memory key-value cache.");

                    // TODO: something should indicate what instance it is?
                    hc.SetDisplayName("Nemcache");
                    hc.SetServiceName("Nemcache");
                });
        }

        private class Service
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

            public void Start()
            {
                var fileNameWithoutExtension = LogFileNameWithoutExtension;
                var extension = LogFileExtension;

                RestoreFromLog(fileNameWithoutExtension, extension);

                var newLog = CleanupAndCreateNew(fileNameWithoutExtension, extension);

                // Subscribing after restore has the effect of compacting the cache.
                var notifications = _memCache.Notifications;
                _archiver = new StreamArchiver(newLog, notifications);

                SetupCompact(notifications);

                _server.Start();
            }

            private void RestoreFromLog(string fileNameWithoutExtension, string extension)
            {
                if (_fileSystem.File.Exists(fileNameWithoutExtension + ".0." + extension))
                {
                    using (
                        var existingLog = new PartitioningFileStream(_fileSystem, fileNameWithoutExtension, extension,
                                                                     _partitionSize, FileAccess.Read))
                    {
                        StreamArchiver.Restore(existingLog, _memCache);
                    }
                }
            }

            private PartitioningFileStream CleanupAndCreateNew(string fileNameWithoutExtension, string extension)
            {
                var generator = new LogFileNameGenerator(fileNameWithoutExtension, extension);
                var existingFiles = generator.GetNextFileName().TakeWhile(fn => _fileSystem.File.Exists(fn));

                // TODO: should avoid deleting before we have backed up the current state. This means will probably need to know when we're logging live state.
                foreach (var existingFile in existingFiles)
                {
                    _fileSystem.File.Delete(existingFile);
                }

                return new PartitioningFileStream(_fileSystem, fileNameWithoutExtension, extension, _partitionSize,
                                                  FileAccess.Read);
            }

            private void SetupCompact(IObservable<ICacheNotification> notifications)
            {
                uint threshold = _partitionSize*10; // TODO: config setting
                var observable = Observable.Create<Unit>(obs =>
                    {
                        int acc = 0;
                        var stores = notifications.OfType<StoreNotification>(); // TODO: Rather not subscribe to this as it will get all state, and when the log gets beyond a certain size it will trigger all the time.
                        return stores.Select(n => n.Data.Length).Subscribe(l =>
                            {
                                acc += l;
                                if (acc > threshold)
                                    obs.OnNext(new Unit());
                            });
                    }).Throttle(TimeSpan.FromMinutes(1));

                observable.Subscribe(_ =>
                    {
                        if (_archiver != null)
                            _archiver.Dispose();

                        var newLog = CleanupAndCreateNew(LogFileNameWithoutExtension, LogFileExtension);
                        _archiver = new StreamArchiver(newLog, notifications);
                    });
            }

            private string LogFileExtension
            {
                get { return Path.GetExtension(_cacheFileName); }
            }

            private string LogFileNameWithoutExtension
            {
                get { return Path.GetFileNameWithoutExtension(_cacheFileName); }
            }

            public void Stop()
            {
                _server.Stop();
                _archiver.Dispose();
            }
        }
    }
}