using System;
using System.IO;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nemcache.Service.IO;
using Nemcache.Service.Persistence;

namespace Nemcache.Service
{
    internal class Service : IDisposable
    {
        private readonly MemCache _memCache;
        private readonly RequestDispatcher _requestDispatcher;
        private readonly RequestResponseTcpServer _server;
        private readonly CacheRestorer _restorer;
        private StreamArchiver _archiver;
        private readonly FileSystemWrapper _fileSystem;
        private readonly CacheRestServer _restListener;

        public Service(ulong capacity, uint port)
        {
            _memCache = new MemCache(capacity, Scheduler.Default);

            _requestDispatcher = new RequestDispatcher(Scheduler.Default, _memCache);
            _server = new RequestResponseTcpServer(IPAddress.Any, (int) port, _requestDispatcher);
            _restListener = new CacheRestServer(_memCache);

            _fileSystem = new FileSystemWrapper();
            const string cachelogBin = "cachelog.bin";
            _restorer = new CacheRestorer(_memCache, _fileSystem, cachelogBin);
        }

        public void Start()
        {
            _restorer.RestoreCache();

            _archiver = new StreamArchiver(_fileSystem.File.Open("cachelog.bin", FileMode.OpenOrCreate, FileAccess.Write));

            _memCache.NewNotifications.ObserveOn(Scheduler.Default).Subscribe(_archiver);

            _server.Start();

            _restListener.Start();
            
        }

        public void Stop()
        {
            _server.Stop();
        }

        public void Dispose()
        {
            Stop();
            _server.Dispose();
            _memCache.Dispose();
        }
    }
}