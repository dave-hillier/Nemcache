using System;
using Nemcache.Service.Notifications;

namespace Nemcache.Service.Persistence
{
    internal interface ICachePersistence : IObserver<ICacheNotification>, IDisposable
    {
        void Restore();
    }
}
