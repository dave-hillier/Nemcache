using Nemcache.Service.Notifications;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nemcache.Service
{
    class StreamArchiver : IDisposable, IObserver<ICacheNotification>
    {
        [ProtoContract]
        public class ArchiveEntry
        {
            [ProtoMember(1)]
            public StoreNotification Store { get; set; }

            [ProtoMember(2)]
            public ClearNotification Clear { get; set; }

            [ProtoMember(3)]
            public TouchNotification Touch { get; set; }

            [ProtoMember(4)]
            public RemoveNotification Remove { get; set; }

        }
        
        private readonly Stream _outputStream;

        public StreamArchiver(Stream outputStream)
        {
            _outputStream = outputStream;
        }

        public static IEnumerable<ArchiveEntry> ReadLog(Stream stream)
        {
            while (stream.Position < stream.Length)
            {
                yield return Serializer.DeserializeWithLengthPrefix<ArchiveEntry>(stream, PrefixStyle.Fixed32);
            }
        }

        // TODO: move to a separate class
        public static void Restore(Stream stream, IMemCache cache)
        {
            var log = ReadLog(stream);
            foreach (var entry in log)
            {
                if (entry.Store != null)
                    cache.Add(entry.Store.Key, entry.Store.Flags, entry.Store.Expiry, entry.Store.Data);
            }
        }

        public void Dispose()
        {
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

        public void OnNext(ICacheNotification value)
        {
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
    }
}