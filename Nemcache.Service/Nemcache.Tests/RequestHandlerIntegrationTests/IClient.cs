using System;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    public interface IClient
    {
        byte[] Send(byte[] p);

        IDisposable OnDisconnect { get; set; } // TODO: replace with a proper callback
    }
}