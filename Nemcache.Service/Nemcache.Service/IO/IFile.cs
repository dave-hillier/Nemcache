using System.IO;

namespace Nemcache.Service.IO
{
    public interface IFile
    {
        Stream Open(string path, FileMode mode, FileAccess access);

        bool Exists(string path);
        long Size(string filename);
        void Delete(string path);
    }
}