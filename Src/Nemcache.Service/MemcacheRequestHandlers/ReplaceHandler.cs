using Nemcache.Storage;
﻿namespace Nemcache.Service.RequestHandlers
{
    internal class ReplaceHandler : SetHandler
    {
        public ReplaceHandler(RequestConverters helpers, IMemCache cache) :
            base(helpers, cache)
        {
        }
    }
}