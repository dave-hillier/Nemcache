namespace Nemcache.Service.Commands
{
    internal class DecrCommand : ICommand
    {
        private readonly IArrayCache _arrayCache;

        public DecrCommand(IArrayCache arrayCache)
        {
            _arrayCache = arrayCache;
        }

        public string Name { get { return "decr"; } }
        public byte[] Execute(IRequest request)
        {
            return _arrayCache.Decrease(request.Key, request.Value);
            
        }
    }
}