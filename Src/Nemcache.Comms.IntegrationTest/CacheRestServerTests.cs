using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Nemcache.Service;

namespace Nemcache.Comms.IntegrationTest
{
    [TestFixture]
    public class CacheRestServerTests
    {
        private CacheRestServer _server;

        [SetUp]
        public void Setup()
        {
            var testHandler = new TestHandler();
            var handlers = new Dictionary<string, IHttpHandler>()
                {
                    {"/cache/test", testHandler}
                };
            _server = new CacheRestServer(handlers, new[]
                {
                    "http://localhost:44444/cache/",
                    "http://localhost:44444/static/"
                });
            _server.Start();
        }

        [TearDown]
        public void Cleanup()
        {
            _server.Stop();
        }

        [Test]
        public void GetTest()
        {
            
            var webClient = new WebClient();
            var response = webClient.DownloadString(new Uri("http://localhost:44444/cache/test"));
            Assert.That(response, Is.EqualTo("Response"));
        }

        [Test]
        public void Test404()
        {

            var webClient = new WebClient();
            bool thrown = false;
            try
            {
                webClient.DownloadString(new Uri("http://localhost:44444/cache/unknown"));
            }
            catch (WebException ex)
            {
                Assert.That(ex.Message.Contains("404"), Is.True);
                thrown = true;
            }
            Assert.That(thrown, Is.True);
        }
    }

    public class TestHandler : HttpHandlerBase
    {
        public override async Task Get(HttpListenerContext context, params string[] match)
        {
            using (var sw = new StreamWriter(context.Response.OutputStream))
            {
                context.Response.StatusCode = 200;
                await sw.WriteAsync("Response");
                await sw.FlushAsync();
            }
            context.Response.Close();
        }
    }
}