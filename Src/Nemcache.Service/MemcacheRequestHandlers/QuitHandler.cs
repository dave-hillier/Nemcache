using Nemcache.Storage;
﻿namespace Nemcache.Service.RequestHandlers
{
    internal class QuitHandler : IRequestHandler
    {
        public void HandleRequest(IRequestContext context)
        {
            context.Close();
        }
    }
}