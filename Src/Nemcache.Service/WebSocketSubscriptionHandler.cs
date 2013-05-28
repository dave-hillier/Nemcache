using System;
using System.Reactive.Subjects;

namespace Nemcache.Service
{
    // TODO: Is this named well? It inteprets commands and setups subscriptions?
    // One per client? Should there be lots of these?
    class WebSocketSubscriptionHandler : ISubject<string>
    {
        public WebSocketSubscriptionHandler()
        {
            
        }

        public void OnNext(string command)
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            throw new NotImplementedException();
        }
    }
}