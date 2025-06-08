using System;
using Nemcache.Storage.Notifications;

namespace Nemcache.Storage.Persistence
{
    public interface ICachePersistence : IObserver<ICacheNotification>, IDisposable
    {
        void Restore();
    }
}
