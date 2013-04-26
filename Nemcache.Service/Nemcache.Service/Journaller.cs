using System;
using System.IO;
using Nemcache.Service.Notifications;

namespace Nemcache.Service
{
    class Journaller
    {
        private Stream _outputStream;

        public Journaller(Stream outputStream, IObservable<ICacheNotification> notifications)
        {
            _outputStream = outputStream;
        }
        
    }
}