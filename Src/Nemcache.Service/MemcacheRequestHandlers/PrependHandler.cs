using Nemcache.Storage;
﻿namespace Nemcache.Service.RequestHandlers
{
    internal class PrependHandler : SetHandler
    {
        public PrependHandler(RequestConverters helpers, IMemCache cache) :
            base(helpers, cache)
        {
        }
    }
}