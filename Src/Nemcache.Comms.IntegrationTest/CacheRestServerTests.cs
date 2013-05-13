using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Comms.IntegrationTest
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
            _server = new CacheRestServer(handlers, null, new[]
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