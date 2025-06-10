using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class StaticFileHttpHandler : HttpHandlerBase
    {
        private readonly string _rootDirectory;
        private readonly Dictionary<string, string> _contentTypes;

        public StaticFileHttpHandler()
            : this(
                ConfigurationManager.AppSettings["StaticFileRoot"] ?? Path.Combine("client", "dist"),
                ParseContentTypes(ConfigurationManager.AppSettings["StaticFileTypes"]))
        {
        }

        public StaticFileHttpHandler(string rootDirectory, Dictionary<string, string> contentTypes)
        {
            _rootDirectory = rootDirectory;
            _contentTypes = contentTypes;
        }

        private static Dictionary<string, string> ParseContentTypes(string? raw)
        {
            var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".html"] = "text/html",
                [".js"] = "application/javascript",
                [".css"] = "text/css"
            };

            if (string.IsNullOrWhiteSpace(raw))
                return defaults;

            var pairs = raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', StringSplitOptions.RemoveEmptyEntries))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim().StartsWith('.') ? p[0].Trim() : "." + p[0].Trim(),
                              p => p[1].Trim(),
                              StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in defaults)
            {
                if (!pairs.ContainsKey(kvp.Key))
                    pairs[kvp.Key] = kvp.Value;
            }

            return pairs;
        }

        public override async Task Get(HttpListenerContext httpContext, params string[] matches)
        {
            var relativePath = matches[0];
            var fullRoot = Path.GetFullPath(_rootDirectory);
            var combined = Path.GetFullPath(Path.Combine(fullRoot, relativePath));

            if (!combined.StartsWith(fullRoot, StringComparison.Ordinal))
            {
                httpContext.Response.StatusCode = 404;
                httpContext.Response.Close();
                return;
            }

            if (!File.Exists(combined))
            {
                httpContext.Response.StatusCode = 404;
                httpContext.Response.Close();
                return;
            }

            var extension = Path.GetExtension(combined);
            if (!_contentTypes.TryGetValue(extension, out var contentType))
            {
                httpContext.Response.StatusCode = 403;
                httpContext.Response.Close();
                return;
            }

            var bytes = await File.ReadAllBytesAsync(combined);
            httpContext.Response.ContentType = contentType;
            httpContext.Response.StatusCode = 200;
            await httpContext.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            httpContext.Response.Close();
        }
    }
}
