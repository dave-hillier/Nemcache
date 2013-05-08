using System.Text;

namespace Nemcache.Service.RequestHandlers
{
    internal class VersionHandler : IRequestHandler
    {
        public void HandleRequest(IRequestContext context)
        {
            var result = Encoding.ASCII.GetBytes(string.Format("Nemcache {0}\r\n", GetType().Assembly.GetName().Version));
            context.ResponseStream.WriteAsync(result, 0, result.Length);
        }
    }
}