using System.Net;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    internal interface IHttpHandler
    {
        Task Get(HttpListenerContext context, params string[] match);
        Task Put(HttpListenerContext context, params string[] match);
        Task Post(HttpListenerContext context, params string[] match);
        Task Delete(HttpListenerContext context, params string[] match);
    }
}