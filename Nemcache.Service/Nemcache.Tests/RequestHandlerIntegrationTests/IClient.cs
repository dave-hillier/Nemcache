using System;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    public interface IClient
    {
        IDisposable OnDisconnect { get; set; }
        byte[] Send(byte[] p);
        // TODO: replace with a proper callback
    }
}