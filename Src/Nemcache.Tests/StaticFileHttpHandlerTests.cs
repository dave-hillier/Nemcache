using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestFixture]
    public class StaticFileHttpHandlerTests
    {
        private CacheRestServer _server = null!;
        private string _rootDir = null!;

        [SetUp]
        public void Setup()
        {
            _rootDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_rootDir);
        }

        [TearDown]
        public void TearDown()
        {
            _server.Stop();
            Directory.Delete(_rootDir, true);
        }

        private void StartServer(IHttpHandler handler, int port)
        {
            var handlers = new Dictionary<string, IHttpHandler>
            {
                {"/static/(.+)", handler}
            };
            _server = new CacheRestServer(handlers, new[] { $"http://localhost:{port}/static/" });
            _server.Start();
        }

        [Test]
        public async Task ServesFileWithCorrectContentType()
        {
            var file = Path.Combine(_rootDir, "index.html");
            await File.WriteAllTextAsync(file, "<html>ok</html>");

            var handler = new StaticFileHttpHandler(_rootDir, new Dictionary<string, string>{{".html","text/html"}});
            StartServer(handler, 45555);

            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:45555/static/index.html");
            var content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.AreEqual("<html>ok</html>", content);
        }

        [Test]
        public async Task DirectoryTraversalReturns404()
        {
            var parent = Directory.GetParent(_rootDir)!;
            var secret = Path.Combine(parent.FullName, "secret.txt");
            await File.WriteAllTextAsync(secret, "top secret");

            var handler = new StaticFileHttpHandler(_rootDir, new Dictionary<string, string>{{".txt","text/plain"}});
            StartServer(handler, 45556);

            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:45556/static/../secret.txt");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test]
        public async Task DisallowedExtensionReturns403()
        {
            var file = Path.Combine(_rootDir, "data.bin");
            await File.WriteAllTextAsync(file, "bin");

            var handler = new StaticFileHttpHandler(_rootDir, new Dictionary<string, string>{{".txt","text/plain"}});
            StartServer(handler, 45557);

            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:45557/static/data.bin");

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
