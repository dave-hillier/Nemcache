using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    class StaticFileHttpHandler : HttpHandlerBase
    {
        public override async Task Get(HttpListenerContext httpContext, params string[] matches)
        {
            // TODO: get the folder from config
            // TODO: allow a set of extensions to be served as various types
            var bytes = File.ReadAllBytes("test.html");
            // TODO: create the path from the matches, escaping .. etc
            httpContext.Response.ContentType = "text/html";
            httpContext.Response.StatusCode = 200;
            await httpContext.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            httpContext.Response.Close();
        }
    }
}