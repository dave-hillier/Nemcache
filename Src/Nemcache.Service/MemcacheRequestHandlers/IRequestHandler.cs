﻿namespace Nemcache.Service.RequestHandlers
{
    public interface IRequestHandler
    {
        void HandleRequest(IRequestContext context);
    }
}