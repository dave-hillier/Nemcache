namespace Nemcache.Client.Builders
{
    public interface IRequestBuilder
    {
        byte[] ToAsciiRequest();
    }
}