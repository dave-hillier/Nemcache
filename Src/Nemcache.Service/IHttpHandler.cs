using System.Net;
using System.Threading.Tasks;

namespace Nemcache.Service
{
    internal interface IHttpHandler
    {
        Task Get(HttpListenerContext context, string[] match);
        Task Put(HttpListenerContext context, string[] match);
        Task Post(HttpListenerContext context, string[] match);
        Task Delete(HttpListenerContext context, string[] match);
    }
}