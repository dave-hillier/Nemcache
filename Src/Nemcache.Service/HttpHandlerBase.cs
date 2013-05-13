using System.Net;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    public class HttpHandlerBase : IHttpHandler
    {
        public virtual async Task Get(HttpListenerContext context, params string[] match)
        {
            NotFoundResponse(context);
        }

        public virtual async Task Put(HttpListenerContext context, params string[] match)
        {
            NotFoundResponse(context);
        }

        public virtual async Task Post(HttpListenerContext context, params string[] match)
        {
            NotFoundResponse(context);
        }

        public virtual async Task Delete(HttpListenerContext context, params string[] match)
        {
            NotFoundResponse(context);
        }

        protected void NotFoundResponse(HttpListenerContext context)
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
        }
    }
}