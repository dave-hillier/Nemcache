namespace Nemcache.Service.IO
{
    internal class FileSystemWrapper : IFileSystem
    {
        public FileSystemWrapper()
        {
            File = new FileWrapper();
        }

        public IFile File { get; private set; }
    }
}