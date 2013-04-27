using Nemcache.Service.Notifications;
using ProtoBuf;
using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nemcache.Service
{
    class Archiver
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

        public Archiver(Stream outputStream, IObservable<ICacheNotification> cacheNotifications)
        {
            _outputStream = outputStream;
            _disposable.Add(cacheNotifications.
                Select(CreateArchiveEntry).
                Subscribe(OnStoreNotification));
        }

        public static void Restore(Stream stream, IMemCache cache)
        {
            while (stream.Position < stream.Length)
            {
                var entry = Serializer.DeserializeWithLengthPrefix<ArchiveEntry>(stream, PrefixStyle.Fixed32);
                if (entry.Store != null)
                    cache.Add(entry.Store.Key, entry.Store.Flags, entry.Store.Expiry, entry.Store.Data);
            }
        }

        private void OnStoreNotification(ArchiveEntry archiveEntry)
        {
            Serializer.SerializeWithLengthPrefix(_outputStream, archiveEntry, PrefixStyle.Fixed32);
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