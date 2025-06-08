using Nemcache.Storage;
﻿using System;

namespace Nemcache.Service.RequestHandlers
{
    internal class ExceptionHandler : IRequestHandler
    {
        public void HandleRequest(IRequestContext context)
        {
            throw new Exception("test exception");
        }
    }
}