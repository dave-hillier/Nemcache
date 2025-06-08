namespace Nemcache.Storage.IO
{
    public interface IFileSystem
    {
        IFile File { get; }
    }
}