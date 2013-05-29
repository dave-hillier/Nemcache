using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
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
        private MemCache _cache;
        private TestScheduler _testScheduler;

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
            _testScheduler = new TestScheduler();
            _cache = new MemCache(10000, _testScheduler);
            _subscriptionHandler = new WebSocketSubscriptionHandler(_cache);
            _client = new Client(_subscriptionHandler);
            _testObserver = _testScheduler.CreateObserver<string>();
        }

        [TestMethod]
        public void SubscribeToNonExistentKeyTest()
        {
            var command = "{\"command\":\"subscribe\",\"key\":\"nosuchkey\"}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"nosuchkey\",\"response\":\"OK\"}"));
        }

        [TestMethod]
        public void SubscribeToEmptyKey()
        {
            var command = "{\"command\":\"subscribe\",\"key\":\"\"}"; 
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"\",\"response\":\"ERROR\"}"));
        }

        [TestMethod]
        public void SubscribeHasCurrentValue()
        {
            SetupValueKey();
            var command = "{\"command\":\"subscribe\",\"key\":\"valuekey\"}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);

            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"valuekey\",\"response\":\"OK\"}"),
                OnNext(0, "{\"value\":\"valuekey\",\"data\":\"1234567890\"}")
                );
        }

        private void SetupValueKey()
        {
            _cache.Add("valuekey", 0, DateTime.MaxValue, Encoding.UTF8.GetBytes("1234567890"));
        }

        [TestMethod]
        public void SubscribeHasTickingValue()
        {
            SetupTickingKey();

            var command = "{\"command\":\"subscribe\",\"key\":\"tickingkey\"}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            _testScheduler.AdvanceBy(2);

            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"tickingkey\",\"response\":\"OK\"}"),
                OnNext(1, "{\"value\":\"tickingkey\",\"data\":\"0\"}"),
                OnNext(2, "{\"value\":\"tickingkey\",\"data\":\"1\"}")
                );
        }

        private void SetupTickingKey()
        {
            Observable.Interval(TimeSpan.FromTicks(1), _testScheduler)
                      .Subscribe(
                          i => { _cache.Store("tickingkey", 0, Encoding.UTF8.GetBytes(i.ToString()), DateTime.MaxValue); });
        }

        [TestMethod]
        public void Unsubscribe()
        {
            SetupTickingKey(); 
            
            var command1 = "{\"command\":\"subscribe\",\"key\":\"tickingkey\"}";
            var command2 = "{\"command\":\"unsubscribe\",\"key\":\"tickingkey\"}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command1);
            _testScheduler.AdvanceBy(1);
            _client.OnNext(command2);
            _testScheduler.AdvanceBy(1);

            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"tickingkey\",\"response\":\"OK\"}"),
                OnNext(1, "{\"value\":\"tickingkey\",\"data\":\"0\"}")
                );            
        }

        [TestMethod]
        public void DisposeSubscription()
        {
            SetupTickingKey();

            var command = "{\"command\":\"subscribe\",\"key\":\"tickingkey\"}";
            var sub = _client.Subscribe(_testObserver);
            _client.OnNext(command);
            sub.Dispose();
            _testScheduler.AdvanceBy(1);
            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"tickingkey\",\"response\":\"OK\"}")
                );
        }

        [TestMethod]
        public void MultiplexedSubscriptions()
        {
            SetupTickingKey();
            SetupValueKey();
            var command1 = "{\"command\":\"subscribe\",\"key\":\"tickingkey\"}";
            var command2 = "{\"command\":\"subscribe\",\"key\":\"valuekey\"}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command1);
            _client.OnNext(command2);

            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"tickingkey\",\"response\":\"OK\"}"),
                OnNext(0, "{\"subscription\":\"valuekey\",\"response\":\"OK\"}"),
                OnNext(0, "{\"value\":\"valuekey\",\"data\":\"1234567890\"}")
                );
        
        }


        [TestMethod]
        public void UnsubscribeOneStillGetValues()
        {
            SetupTickingKey();
            SetupValueKey();
            _client.Subscribe(_testObserver);

            var command1 = "{\"command\":\"subscribe\",\"key\":\"valuekey\"}";
            _client.OnNext(command1);
            
            var command2 = "{\"command\":\"subscribe\",\"key\":\"tickingkey\"}";
            _client.OnNext(command2);
            
            _testObserver.Messages.Clear();

            var command3 = "{\"command\":\"unsubscribe\",\"key\":\"valuekey\"}";
            _client.OnNext(command3);

            _testScheduler.AdvanceBy(1);
            _testObserver.Messages.AssertEqual(
                OnNext(1, "{\"value\":\"tickingkey\",\"data\":\"0\"}"));
        }

        // TODO: disconnects?
        // TODO: double subscribe?
        // TODO: multiple client tests
        // TODO: clear value, flush all
        // Do I want?
        // TODO: Content type encoding - in subscription? In response?
        // TODO: pattern matching subscribe
        // TODO: subscribe to all?
        // TODO: multikey subscribe?
    }
}