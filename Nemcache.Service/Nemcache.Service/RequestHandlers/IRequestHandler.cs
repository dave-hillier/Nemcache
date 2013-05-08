namespace Nemcache.Service.RequestHandlers
{
    internal interface IRequestHandler
    {
        void HandleRequest(IRequestContext context);
    }
}