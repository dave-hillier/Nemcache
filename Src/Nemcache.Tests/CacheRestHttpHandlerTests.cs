using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Nemcache.Service;
using Nemcache.Storage;

namespace Nemcache.Tests
{
    [TestFixture]
    public class CacheRestHttpHandlerTests
    {
        private CacheRestServer _server = null!;

        private void StartServer(IMemCache cache, int port)
        {
            var handler = new CacheRestHttpHandler(cache);
            var handlers = new Dictionary<string, IHttpHandler>
            {
                {"/cache/(.+)", handler}
            };
            _server = new CacheRestServer(handlers, new[] { $"http://localhost:{port}/cache/" });
            _server.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _server?.Stop();
        }

        [Test]
        public async Task PutAndGet_WithContentType_ReturnsSameContentType()
        {
            var cache = new MemCache(10000);
            StartServer(cache, 46660);

            using var client = new HttpClient();
            var content = new StringContent("hello world", Encoding.UTF8, "text/custom");
            var putResponse = await client.PutAsync("http://localhost:46660/cache/mykey", content);
            Assert.AreEqual(HttpStatusCode.OK, putResponse.StatusCode);

            var getResponse = await client.GetAsync("http://localhost:46660/cache/mykey");
            var body = await getResponse.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.AreEqual("text/custom", getResponse.Content.Headers.ContentType?.MediaType);
            Assert.AreEqual("hello world", body);
        }

        [Test]
        public async Task Get_MissingContentType_Returns404()
        {
            var cache = new MemCache(10000);
            cache.Store("orphan", 0, Encoding.UTF8.GetBytes("data"), DateTime.MaxValue);
            StartServer(cache, 46661);

            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:46661/cache/orphan");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
