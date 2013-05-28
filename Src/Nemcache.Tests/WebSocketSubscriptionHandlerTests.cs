using System;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class WebSocketSubscriptionHandlerTests : ReactiveTest
    {
        private Client _client;
        private ITestableObserver<string> _testObserver;
        private WebSocketSubscriptionHandler _subscriptionHandler;

        // TODO: is there any point in this class?
        // It could go to network....
        class Client : ISubject<string> 
        {
            private readonly WebSocketSubscriptionHandler _handler;

            public Client(WebSocketSubscriptionHandler handler)
            {
                _handler = handler;
            }

            public void OnNext(string command)
            {
                _handler.OnNext(command);
            }

            public void OnError(Exception error)
            {
                _handler.OnError(error);
            }

            public void OnCompleted()
            {
                _handler.OnCompleted();
            }

            public IDisposable Subscribe(IObserver<string> observer)
            {
                return _handler.Subscribe(observer);
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _subscriptionHandler = new WebSocketSubscriptionHandler();
            _client = new Client(_subscriptionHandler);
            var testScheduler = new TestScheduler();
            _testObserver = testScheduler.CreateObserver<string>();
        }

        [TestMethod]
        public void SubscribeToNonExistentKeyTest()
        {
            var command = "{'command':'subscribe','key':'nosuchkey'}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            _testObserver.Messages.AssertEqual(
                OnNext(1, "{'subscription':'nosuchkey','response':'OK'}"));
        }

        [TestMethod]
        public void SubscribeToEmptyKey()
        {
            var command = "{'command':'subscribe','key':''}"; // TODO: or missing entirely?
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            _testObserver.Messages.AssertEqual(
                OnNext(1, "{'subscription':'nosuchkey','response':'ERROR: Empty Key'}"));
        }

        [TestMethod]
        public void SubscribeHasCurrentValue()
        {
            var command = "{'command':'subscribe','key':'valuekey'}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            // TODO: assert what?
        }

        [TestMethod]
        public void SubscribeHasTickingValue()
        {
            var command = "{'command':'subscribe','key':'tickingkey'}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            // TODO: assert what?
            // First no value, then value after interval
        }

        [TestMethod]
        public void Unsubscribe()
        {
            var command = "{'command':'unsubscribe','key':'tickingkey'}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            // TODO: assert what?
        }

        [TestMethod]
        public void DisposeSubscription()
        {
            var command = "{'command':'subscribe','key':'tickingkey'}";
            var sub = _client.Subscribe(_testObserver);
            _client.OnNext(command);
            sub.Dispose();
            // TODO: assert what?
        }

        [TestMethod]
        public void MultiplexedSubscriptions()
        {
            var command1 = "{'command':'subscribe','key':'tickingkey'}";
            var command2 = "{'command':'subscribe','key':'valuekey'}";
            var sub = _client.Subscribe(_testObserver);
            _client.OnNext(command1);
            _client.OnNext(command2);
            sub.Dispose();
            // TODO: assert what?
        }

        // TODO: multiple client tests
        // Do I want?
        // TODO: Content type encoding - in subscription? In response?
        // TODO: pattern matching subscribe
        // TODO: subscribe to all?
        // TODO: multikey subscribe?
    }
}