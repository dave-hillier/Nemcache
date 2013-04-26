namespace Nemcache.Client.Builders
{
    internal interface IRequestBuilder
    {
        byte[] ToAsciiRequest();
    }
}