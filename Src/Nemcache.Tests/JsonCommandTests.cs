using System;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nemcache.Tests
{
    [TestClass]
    public class JsonCommandTests
    {
        private Client _client;
        private ITestableObserver<string> _testObserver;

        class Client : ISubject<string>
        {
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

        [TestInitialize]
        public void Setup()
        {
            _client = new Client();
            var testScheduler = new TestScheduler();
            _testObserver = testScheduler.CreateObserver<string>();
        }

        [TestMethod]
        public void SubscribeToNonExistentKeyTest()
        {
            var command = "{'command':'subscribe','key':'nosuchkey'}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            // TODO: assert what?
        }

        [TestMethod]
        public void SubscribeToEmptyKey()
        {
            var command = "{'command':'subscribe','key':''}";
            _client.Subscribe(_testObserver);
            _client.OnNext(command);
            // TODO: assert what?
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

        // TODO: Content type encoding.

        // Do I want?
        // TODO: subscribe to all?
        // TODO: multikey subscribe?
    }
}