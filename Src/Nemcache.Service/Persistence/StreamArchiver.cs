using System;
using System.IO;
using Nemcache.Service.Notifications;
using ProtoBuf;

namespace Nemcache.Service.Persistence
{
    internal class StreamArchiver : IObserver<ICacheNotification>
    {
        private readonly Stream _outputStream;

        public StreamArchiver(Stream outputStream)
        {
            _outputStream = outputStream;
        }

        public void OnNext(ICacheNotification value)
        {
            if (value is RetrieveNotification)
                return;

            var entry = CreateArchiveEntry(value);
            OnNotification(entry);
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

        private void OnNotification(ArchiveEntry archiveEntry)
        {
            Serializer.SerializeWithLengthPrefix(_outputStream, archiveEntry, PrefixStyle.Fixed32);
            _outputStream.FlushAsync();
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