using System.Text;
using NUnit.Framework;
using Nemcache.Client.Builders;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    /// <summary>
    /// Bug: MutateRequestBuilder.ToAsciiRequest appends the boolean value "True"
    /// instead of the protocol keyword "noreply" when NoReply() is called.
    /// Line 23: result += " " + _noReply; // _noReply is a bool, so this emits " True"
    /// Should be: result += " noreply";
    /// See MutateRequestBuilder.cs line 23.
    /// </summary>
    [TestFixture]
    public class MutateRequestBuilderNoReplyBugTest
    {
        [Test]
        public void NoReplyShouldEmitProtocolKeywordNotBooleanString()
        {
            var builder = new MutateRequestBuilder("incr", "mykey", 1);
            builder.NoReply();

            byte[] request = builder.ToAsciiRequest();
            string text = Encoding.ASCII.GetString(request);

            // Per memcached protocol, noreply token must be the literal string "noreply"
            Assert.AreEqual("incr mykey 1 noreply\r\n", text,
                "MutateRequestBuilder should emit 'noreply' not 'True'");
        }
    }
}
