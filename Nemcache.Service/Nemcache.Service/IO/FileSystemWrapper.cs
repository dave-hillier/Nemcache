namespace Nemcache.Service.IO
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