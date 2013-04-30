using System.IO;

namespace Nemcache.Service.FileSystem
{
    class FileWrapper : IFile
    {
        public Stream Open(string path, FileMode mode, FileAccess access)
        {
            return File.Open(path, mode, access);
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }
    }
}