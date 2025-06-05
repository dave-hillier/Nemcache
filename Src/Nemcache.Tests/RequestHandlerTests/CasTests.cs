using NUnit.Framework;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    [TestFixture]
    public class CasTests
    {
        private IClient _client;

        private byte[] Dispatch(byte[] p)
        {
            return _client.Send(p);
        }

        [SetUp]
        public void Setup()
        {
            _client = new LocalRequestHandlerWithTestScheduler();
        }

        // TODO: cas capacity checks
        [Test]
        public void CasNoPrevious()
        {
            var casBuilder = new CasRequestBuilder("key", "value");
            ulong lastCas = 123;
            casBuilder.WithCasUnique(lastCas);

            var response = Dispatch(casBuilder.ToAsciiRequest());
            Assert.AreEqual("STORED\r\n", response.ToAsciiString());

            var getBuilder = new GetRequestBuilder("get", "key");
            var getResponse = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 5 123\r\nvalue\r\nEND\r\n", getResponse.ToAsciiString());
        }

        [Test]
        public void CasUpdatePreviousResponse()
        {
            var casBuilder1 = new CasRequestBuilder("key", "value1");
            casBuilder1.WithCasUnique(567);
            Dispatch(casBuilder1.ToAsciiRequest());

            var casBuilder2 = new CasRequestBuilder("key", "value2");
            casBuilder2.WithCasUnique(567);

            var response2 = Dispatch(casBuilder2.ToAsciiRequest());

            Assert.AreEqual("STORED\r\n", response2.ToAsciiString());
        }

        [Test]
        public void CasUpdatePreviousModifiedResponse()
        {
            var casBuilder1 = new CasRequestBuilder("key", "value1");
            casBuilder1.WithCasUnique(789);
            Dispatch(casBuilder1.ToAsciiRequest());

            var casBuilder2 = new CasRequestBuilder("key", "value2");
            casBuilder2.WithCasUnique(567);

            var response2 = Dispatch(casBuilder2.ToAsciiRequest());
            Assert.AreEqual("EXISTS\r\n", response2.ToAsciiString());
        }


        [Test]
        public void CasUpdatePreviousValue()
        {
            var casBuilder1 = new CasRequestBuilder("key", "value1");
            casBuilder1.WithCasUnique(567);
            Dispatch(casBuilder1.ToAsciiRequest());

            var casBuilder2 = new CasRequestBuilder("key", "value2");
            casBuilder2.WithCasUnique(567);

            Dispatch(casBuilder2.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var getResponse = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 6 567\r\nvalue2\r\nEND\r\n", getResponse.ToAsciiString());
        }

        [Test]
        public void CasUpdatePreviousModifiedValue()
        {
            var casBuilder1 = new CasRequestBuilder("key", "value1");
            casBuilder1.WithCasUnique(789);
            Dispatch(casBuilder1.ToAsciiRequest());

            var casBuilder2 = new CasRequestBuilder("key", "value2");
            casBuilder2.WithCasUnique(567);

            Dispatch(casBuilder2.ToAsciiRequest());

            var getBuilder = new GetRequestBuilder("get", "key");
            var getResponse = Dispatch(getBuilder.ToAsciiRequest());
            Assert.AreEqual("VALUE key 0 6 789\r\nvalue1\r\nEND\r\n", getResponse.ToAsciiString());
        }
    }
}