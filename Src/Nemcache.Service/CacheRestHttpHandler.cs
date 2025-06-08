using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Nemcache.Storage;
namespace Nemcache.Service
{
    class CacheRestHttpHandler : HttpHandlerBase
    {
        private readonly IMemCache _cache;

        public CacheRestHttpHandler(IMemCache cache)
        {
            _cache = cache;
        }

        public override async Task Get(HttpListenerContext httpContext, params string[] matches)
        {
            var key = matches[0];
            CacheEntry entry;
            var hasValue = _cache.TryGet(key, out entry);
            if (!hasValue)
            {
                httpContext.Response.StatusCode = 404;
                httpContext.Response.Close();
            }
            else
            {
                var contentKey = string.Format("content:{0}", key);
                CacheEntry cacheEntry;
                var hasContent = _cache.TryGet(contentKey, out cacheEntry);
                
                string contentType = httpContext.Request.ContentType;
                if (hasContent)
                {
                    var contentTypeBytes = cacheEntry.Data;
                    //httpContext.Request.ContentType
                    contentType = Encoding.UTF8.GetString(contentTypeBytes);
                }

                httpContext.Response.ContentType = contentType;

                var value = entry.Data;// TODO: does this need converting, based upon content type
                var outputStream = httpContext.Response.OutputStream;
                await outputStream.WriteAsync(value, 0, value.Length/*, _cancellationTokenSource.Token*/);
                httpContext.Response.Close();
            }
        }

        public override async Task Put(HttpListenerContext context, params string[] matches)
        {
            // TODO: content type...
            var key = matches[0];
            var streamReader = new StreamReader(context.Request.InputStream);
            var body = await streamReader.ReadToEndAsync();

            _cache.Store(key, 0, Encoding.UTF8.GetBytes(body), DateTime.MaxValue);

            byte[] response = Encoding.UTF8.GetBytes("STORED\r\n");
            context.Response.StatusCode = 200;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = response.Length;

            var output = context.Response.OutputStream;
            await output.WriteAsync(response, 0, response.Length);
            context.Response.Close();
        }
    }
}