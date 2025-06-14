﻿using System;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Nemcache.Storage;

using Nemcache.Service;
namespace Nemcache.Tests
{
    // TODO: these tests are fragile as they depend on Json structure
    [TestFixture]
    public class WebSocketSubscriptionHandlerTests : ReactiveTest
    {
        private Client _client;
        private ITestableObserver<string> _testObserver;
        private CacheEntrySubscriptionHandler _subscriptionHandler;
        private MemCache _cache;
        private TestScheduler _testScheduler;

        // TODO: is there any point in this class?
        // It could go to network....
        class Client : ISubject<string> 
        {
            private readonly CacheEntrySubscriptionHandler _handler;
            public Client(CacheEntrySubscriptionHandler handler)
            {
                _handler = handler;
            }

            public void OnNext(string command)
            {
                _handler.HandleMessage(command);
            }

            public void OnError(Exception error)
            {
                
            }

            public void OnCompleted()
            {
            }

            public IDisposable Subscribe(IObserver<string> observer)
            {
                return Disposable.Empty;
            }
        }

        private void SetupValueKey(string key = "valuekey")
        {
            _cache.Add(key, 0, DateTime.MaxValue, Encoding.UTF8.GetBytes("1234567890"));
        }

        private void SetupTickingKey(string key = "tickingkey")
        {
            Observable.Interval(TimeSpan.FromTicks(1), _testScheduler)
                      .Subscribe(
                          i => _cache.Store(key, 0,
                              Encoding.UTF8.GetBytes(i.ToString(CultureInfo.InvariantCulture)), DateTime.MaxValue));
        }

        [SetUp]
        public void Setup()
        {
            _testScheduler = new TestScheduler();
            _cache = new MemCache(10000, _testScheduler);
            _testObserver = _testScheduler.CreateObserver<string>();
            _subscriptionHandler = new CacheEntrySubscriptionHandler(_cache, _testObserver);
            _client = new Client(_subscriptionHandler);
        }

        [Test]
        public void SubscribeToNonExistentKeyTest()
        {
            var command = "{\"command\":\"subscribe\",\"key\":\"nosuchkey\"}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"nosuchkey\",\"response\":\"OK\"}"));
        }

        [Test]
        public void SubscribeToEmptyKey()
        {
            var command = "{\"command\":\"subscribe\",\"key\":\"\"}"; 
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"\",\"response\":\"ERROR\"}"));
        }

        [Test]
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

        [Test]
        public void SubscribeHasTickingValue()
        {
            SetupTickingKey();

            _client.Subscribe(_testObserver);
            var command = "{\"command\":\"subscribe\",\"key\":\"tickingkey\"}";
            _client.OnNext(command);
            _testScheduler.AdvanceBy(2);

            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"tickingkey\",\"response\":\"OK\"}"),
                OnNext(1, "{\"value\":\"tickingkey\",\"data\":\"0\"}"),
                OnNext(2, "{\"value\":\"tickingkey\",\"data\":\"1\"}")
                );
        }


        [Test]
        public void Unsubscribe()
        {
            SetupTickingKey();

            _client.Subscribe(_testObserver);
            var command1 = "{\"command\":\"subscribe\",\"key\":\"tickingkey\"}";
            _client.OnNext(command1);
            _testScheduler.AdvanceBy(1);
            var command2 = "{\"command\":\"unsubscribe\",\"key\":\"tickingkey\"}";
            _client.OnNext(command2);
            _testScheduler.AdvanceBy(1);

            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"tickingkey\",\"response\":\"OK\"}"),
                OnNext(1, "{\"value\":\"tickingkey\",\"data\":\"0\"}"),
                OnNext(1, "{\"subscription\":\"tickingkey\",\"response\":\"UNSUBSCRIBED\"}")
                );
        }

        [Test]
        public void MultiplexedSubscriptions()
        {
            SetupTickingKey();
            SetupValueKey();
            _client.Subscribe(_testObserver);

            var command1 = "{\"command\":\"subscribe\",\"key\":\"tickingkey\"}";
            _client.OnNext(command1);
            var command2 = "{\"command\":\"subscribe\",\"key\":\"valuekey\"}";
            _client.OnNext(command2);

            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"tickingkey\",\"response\":\"OK\"}"),
                OnNext(0, "{\"subscription\":\"valuekey\",\"response\":\"OK\"}"),
                OnNext(0, "{\"value\":\"valuekey\",\"data\":\"1234567890\"}")
                );
        
        }


        [Test]
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
                OnNext(0, "{\"subscription\":\"valuekey\",\"response\":\"UNSUBSCRIBED\"}"),
                OnNext(1, "{\"value\":\"tickingkey\",\"data\":\"0\"}"));
        }

        [Test]
        public void RemovalNotification()
        {
            SetupValueKey();
            _client.Subscribe(_testObserver);
            var cmd = "{\"command\":\"subscribe\",\"key\":\"valuekey\"}";
            _client.OnNext(cmd);

            _cache.Remove("valuekey");

            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"valuekey\",\"response\":\"OK\"}"),
                OnNext(0, "{\"value\":\"valuekey\",\"data\":\"1234567890\"}"),
                OnNext(0, "{\"remove\":\"valuekey\"}")
                );
        }

        [Test]
        public void MultipleClientsReceiveUpdates()
        {
            SetupTickingKey();
            var observer2 = _testScheduler.CreateObserver<string>();
            var handler2 = new CacheEntrySubscriptionHandler(_cache, observer2);
            var client2 = new Client(handler2);

            _client.Subscribe(_testObserver);
            client2.Subscribe(observer2);

            var command = "{\"command\":\"subscribe\",\"key\":\"tickingkey\"}";
            _client.OnNext(command);
            client2.OnNext(command);

            _testScheduler.AdvanceBy(1);

            _testObserver.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"tickingkey\",\"response\":\"OK\"}"),
                OnNext(1, "{\"value\":\"tickingkey\",\"data\":\"0\"}")
                );
            observer2.Messages.AssertEqual(
                OnNext(0, "{\"subscription\":\"tickingkey\",\"response\":\"OK\"}"),
                OnNext(1, "{\"value\":\"tickingkey\",\"data\":\"0\"}")
                );
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
        // TODO: conflation add a parameter?
        // TODO: slow consumer - replace old values?
    }
}