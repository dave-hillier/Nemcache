using NUnit.Framework;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    /// <summary>
    /// Bug: MemCache.Mutate performs unsigned subtraction without underflow protection.
    /// When decrementing a value by more than its current value, the ulong wraps around
    /// to a huge number instead of clamping to 0.
    /// Per memcached protocol: "decr" must not go below 0.
    /// See MemCache.cs line 107.
    /// </summary>
    [TestFixture]
    public class DecrementUnderflowBugTest
    {
        private IClient _client;

        private byte[] Dispatch(byte[] p) => _client.Send(p);

        [SetUp]
        public void Setup()
        {
            _client = new LocalRequestHandlerWithTestScheduler();
        }

        [Test]
        public void DecrementBelowZeroShouldClampToZero()
        {
            // Store value "5" then decrement by 10
            // Per memcached protocol, result should be 0 (clamped), not underflow
            var storageBuilder = new StoreRequestBuilder("set", "key", "5");
            Dispatch(storageBuilder.ToAsciiRequest());

            var builder = new MutateRequestBuilder("decr", "key", 10);
            var response = Dispatch(builder.ToAsciiRequest());

            Assert.AreEqual("0\r\n", response.ToAsciiString(),
                "Decrementing below zero should clamp to 0, not wrap around");
        }

        [Test]
        public void DecrementToExactlyZeroShouldReturnZero()
        {
            // Store value "5" then decrement by 5
            var storageBuilder = new StoreRequestBuilder("set", "key", "5");
            Dispatch(storageBuilder.ToAsciiRequest());

            var builder = new MutateRequestBuilder("decr", "key", 5);
            var response = Dispatch(builder.ToAsciiRequest());

            Assert.AreEqual("0\r\n", response.ToAsciiString());
        }
    }
}
