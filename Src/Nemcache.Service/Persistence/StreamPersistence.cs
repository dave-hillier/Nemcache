using System;
using Nemcache.Service.Notifications;

namespace Nemcache.Service.Persistence
{
    internal class StreamPersistence : ICachePersistence
    {
        private readonly StreamArchiver _archiver;
        private readonly CacheRestorer _restorer;

        public StreamPersistence(StreamArchiver archiver, CacheRestorer restorer)
        {
            _archiver = archiver;
            _restorer = restorer;
        }

        public void Restore() => _restorer.RestoreCache();
        public void OnNext(ICacheNotification value) => _archiver.OnNext(value);
        public void OnError(Exception error) => _archiver.OnError(error);
        public void OnCompleted() => _archiver.OnCompleted();
        public void Dispose() { }
    }
}
