using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestClass]
    public class AppendTests
    {
        private IClient _client;

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        public IClient Client { get; set; }

        [TestInitialize]
        public void Setup()
        {
            _client = Client ?? new LocalRequestHandlerWithTestScheduler();
        }

        [TestMethod]
        public void AppendToEmpty()
        {
            var appendBuilder = new StoreRequestBuilder("append", "key", "value");

            var response = Dispatch(appendBuilder.ToAsciiRequest());

            Assert.AreEqual("NOT_STORED\r\n", response.ToAsciiString());
        }


        [TestMethod]
        public void AppendNoReply()
        {
            var appendBuilder = new StoreRequestBuilder("append", "key", "value");
            appendBuilder.NoReply();

            var response = Dispatch(appendBuilder.ToAsciiRequest());

            Assert.AreEqual("", response.ToAsciiString());
            
        }


        [TestMethod]
        public void AppendToExisting()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(setBuilder.ToAsciiRequest());

            var appendBuilder = new StoreRequestBuilder("append", "key", "value");
            var response = Dispatch(appendBuilder.ToAsciiRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        // TODO: get value of no reply
        /*[TestMethod]
        public void GetValueOfAppendToEmpty()
        {
            var appendBuilder = new StoreRequestBuilder("append", "key", "value");
            Dispatch(appendBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }*/

        [TestMethod]
        public void GetValueOfAppendToExisting()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "first");
            Dispatch(setBuilder.ToAsciiRequest());

            var appendBuilder = new StoreRequestBuilder("append", "key", " second");
            Dispatch(appendBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 12\r\nfirst second\r\nEND\r\n", response.ToAsciiString());
        }

 
        [TestMethod]
        public void PrependToEmpty()
        {
            var prependBuilder = new StoreRequestBuilder("prepend", "key", "value");

            var response = Dispatch(prependBuilder.ToAsciiRequest());

            Assert.AreEqual("NOT_STORED\r\n", response.ToAsciiString());
        }

        [TestMethod]
        public void PrependNoReply()
        {
            var prependBuilder = new StoreRequestBuilder("prepend", "key", "value");
            prependBuilder.NoReply();

            var response = Dispatch(prependBuilder.ToAsciiRequest());

            Assert.AreEqual("", response.ToAsciiString());
        }

        [TestMethod]
        public void PrependToExisting()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "value");

            Dispatch(setBuilder.ToAsciiRequest());

            var prependBuilder = new StoreRequestBuilder("prepend", "key", "value");

            var response = Dispatch(prependBuilder.ToAsciiRequest());

            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }
        /*
        [TestMethod]
        public void GetValueOfPrependToEmpty()
        {
            var prependBuilder = new StoreRequestBuilder("prepend", "key", "value");

            Dispatch(prependBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 5\r\nvalue\r\nEND\r\n", response.ToAsciiString());
        }
        */
        // TODO: prepend ignores existing flags and exp

        [TestMethod]
        public void GetValueOfPrependToExisting()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "first");
            Dispatch(setBuilder.ToAsciiRequest());

            var prependBuilder = new StoreRequestBuilder("prepend", "key", "second ");
            Dispatch(prependBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 12\r\nsecond first\r\nEND\r\n", response.ToAsciiString());
        }
    }
}