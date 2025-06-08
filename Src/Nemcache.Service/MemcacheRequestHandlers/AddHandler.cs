using Nemcache.Storage;
﻿namespace Nemcache.Service.RequestHandlers
{
    internal class AddHandler : SetHandler
    {
        public AddHandler(RequestConverters helpers, IMemCache cache) :
            base(helpers, cache)
        {
        }
    }
}