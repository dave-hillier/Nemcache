﻿using NUnit.Framework;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestFixture]
    public class ReplaceTests
    {
        private IClient _client;

        public IClient Client { get; set; }

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        [SetUp]
        public void Setup()
        {
            _client = Client ?? new LocalRequestHandlerWithTestScheduler();
        }

        [Test]
        public void ReplaceToEmpty()
        {
            var replaceBuilder = new StoreRequestBuilder("replace", "key", "value");

            var response = Dispatch(replaceBuilder.ToAsciiRequest());

            Assert.AreEqual("NOT_STORED\r\n", response.ToAsciiString());
        }

        [Test]
        public void ReplaceNoReply()
        {
            var replaceBuilder = new StoreRequestBuilder("replace", "key", "value");
            replaceBuilder.NoReply();

            var response = Dispatch(replaceBuilder.ToAsciiRequest());

            Assert.AreEqual("", response.ToAsciiString());
        }

        [Test]
        public void ReplaceToExisting()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "value");
            Dispatch(setBuilder.ToAsciiRequest());

            var replaceBuilder = new StoreRequestBuilder("replace", "key", "value");
            var response = Dispatch(replaceBuilder.ToAsciiRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());
        }

        // TODO: replace ignores existing flags and exp

        [Test]
        public void GetValueOfReplaceToExisting()
        {
            var setBuilder = new StoreRequestBuilder("set", "key", "first");
            Dispatch(setBuilder.ToAsciiRequest());

            var replaceBuilder = new StoreRequestBuilder("replace", "key", "second");
            Dispatch(replaceBuilder.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var response = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 6\r\nsecond\r\nEND\r\n", response.ToAsciiString());
        }
    }
}