namespace Nemcache.Service
{
    internal interface ICommand
    {
        string Name { get; }

        byte[] Execute(IRequest request);
    }
}