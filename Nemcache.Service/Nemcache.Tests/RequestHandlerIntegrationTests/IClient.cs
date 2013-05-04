using System;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    interface IClient
    {
        byte[] Send(byte[] p);

        IDisposable OnDisconnect { get; set; } // TODO: replace with a proper callback
    }
}