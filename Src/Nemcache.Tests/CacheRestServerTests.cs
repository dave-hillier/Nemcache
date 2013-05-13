using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class CacheRestServerTests
    {
        private CacheRestServer _server;

        [TestInitialize]
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

        [TestCleanup]
        public void Cleanup()
        {
            _server.Stop();
        }

        [TestMethod]
        public void GetTest()
        {
            
            var webClient = new WebClient();
            var response = webClient.DownloadString(new Uri("http://localhost:44444/cache/test"));
            Assert.AreEqual("Response", response);
        }

        [TestMethod]
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
                Assert.IsTrue(ex.Message.Contains("404"));
                thrown = true;
            }
            Assert.IsTrue(thrown);
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