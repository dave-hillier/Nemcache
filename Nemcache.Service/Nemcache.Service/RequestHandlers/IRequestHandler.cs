namespace Nemcache.Service.RequestHandlers
{
    interface IRequestHandler
    {
        void HandleRequest(IRequestContext context);
    }
}