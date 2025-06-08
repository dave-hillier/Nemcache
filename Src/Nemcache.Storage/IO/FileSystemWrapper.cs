namespace Nemcache.Storage.IO
{
    public class FileSystemWrapper : IFileSystem
    {
        public FileSystemWrapper()
        {
            File = new FileWrapper();
        }

        public IFile File { get; private set; }
    }
}