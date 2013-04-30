namespace Nemcache.Service.FileSystem
{
    class FileSystemWrapper : IFileSystem
    {
        public FileSystemWrapper()
        {
            File = new FileWrapper();
        }
        public IFile File { get; private set; }
    }
}