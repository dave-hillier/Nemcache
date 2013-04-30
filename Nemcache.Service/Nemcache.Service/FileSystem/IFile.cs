using System.IO;

namespace Nemcache.Service.FileSystem
{
    public interface IFile
    {
        Stream Open(string path, FileMode mode, FileAccess access);

        bool Exists(string path);
    }
}