using Nemcache.Storage;
﻿namespace Nemcache.Service.RequestHandlers
{
    internal class AppendHandler : SetHandler
    {
        public AppendHandler(RequestConverters helpers, IMemCache cache) :
            base(helpers, cache)
        {
        }
    }
}