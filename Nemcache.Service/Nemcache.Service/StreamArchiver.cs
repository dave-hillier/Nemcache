using Nemcache.Service.Notifications;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nemcache.Service
{
    // TODO: add handling for large streams
    // TODO: add compacting
    class StreamArchiver : IDisposable
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
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public StreamArchiver(Stream outputStream, IObservable<ICacheNotification> cacheNotifications)
        {
            _outputStream = outputStream;
            _disposable.Add(cacheNotifications.
                Select(CreateArchiveEntry).
                Subscribe(OnNotification));
        }

        public static IEnumerable<ArchiveEntry> ReadLog(Stream stream)
        {
            while (stream.Position < stream.Length)
            {
                yield return Serializer.DeserializeWithLengthPrefix<ArchiveEntry>(stream, PrefixStyle.Fixed32);
            }
        }

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
            // TODO: don't cast 4 times
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