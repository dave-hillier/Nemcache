using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Nemcache.Storage;
using Nemcache.Service;
using Nemcache.Service.RequestHandlers;

namespace Nemcache.Tests
{
    [TestFixture]
    public class CacheRestServerTests
    {
        private CacheRestServer _server;

        [SetUp]
        public void Setup()
        {
            var testHandler = new TestHandler();
            var handlers = new Dictionary<string, IHttpHandler>
                {
                    {"/cache/base", new NoOverridesHandler()},
                    {"/cache/test", testHandler},
                    {"/cache/regex/([a-z]+)/([0-9]+)", testHandler}
                };
            var prefixes = new[] { "http://localhost:44444/cache/", "http://localhost:44444/static/" };
            _server = new CacheRestServer(handlers, prefixes);
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
        public void GetRegexTest()
        {
            var webClient = new WebClient();
            webClient.DownloadString(new Uri("http://localhost:44444/cache/regex/param/123"));
            Assert.That(TestHandler.LastParams.Length, Is.EqualTo(2));
            Assert.That(TestHandler.LastParams[0], Is.EqualTo("param"));
            Assert.That(TestHandler.LastParams[1], Is.EqualTo("123"));
        }

        [Test]
        public void PutTest()
        {
            var webClient = new WebClient();
            var response = webClient.UploadString(new Uri("http://localhost:44444/cache/test"), "PUT", "data");

            Assert.That(response, Is.EqualTo("Response"));
            Assert.That(TestHandler.LastRequest, Is.EqualTo("data"));
        }

        [Test]
        public void PostTest()
        {
            var webClient = new WebClient();
            var response = webClient.UploadString(new Uri("http://localhost:44444/cache/test"), "POST", "body");

            Assert.That(response, Is.EqualTo("Response"));
            Assert.That(TestHandler.LastRequest, Is.EqualTo("body"));
        }

        [Test]
        public void DeleteTest()
        {
            var webClient = new WebClient();
            var response = webClient.UploadString(new Uri("http://localhost:44444/cache/test"), "DELETE", "thing!");

            Assert.That(response, Is.EqualTo("Response"));
            Assert.That(TestHandler.LastRequest, Is.EqualTo("thing!"));
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

    public class NoOverridesHandler : HttpHandlerBase
    {
    }

    public class TestHandler : HttpHandlerBase
    {
        public static string[] LastParams { get; set; }

        public override async Task Get(HttpListenerContext context, params string[] match)
        {
            LastParams = match;
            using (var sw = new StreamWriter(context.Response.OutputStream))
            {
                context.Response.StatusCode = 200;
                sw.Write("Response");
                sw.Flush();
            }
            context.Response.Close();
        }

        public static string LastRequest { get; set; }

        public override async Task Put(HttpListenerContext context, params string[] match)
        {
            LastParams = match;
            using (var sr = new StreamReader(context.Request.InputStream))
            using (var sw = new StreamWriter(context.Response.OutputStream))
            {
                LastRequest = sr.ReadToEnd();
                context.Response.StatusCode = 200;
                sw.Write("Response");
                sw.Flush();
            }
            context.Response.Close();
        }

        public override async Task Post(HttpListenerContext context, string[] match)
        {
            await Put(context, match);
        }
        public override async Task Delete(HttpListenerContext context, string[] match)
        {
            await Put(context, match);
        }
    }
}