using System;

namespace Nemcache.Tests.RequestHandlerIntegrationTests
{
    public interface IClient
    {
        Action OnDisconnect { get; set; }
        byte[] Send(byte[] p);
        // TODO: replace with a proper callback
    }
}