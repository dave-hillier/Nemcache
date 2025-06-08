using System.IO;

namespace Nemcache.Storage.IO
{
    internal class FileWrapper : IFile
    {
        public Stream Open(string path, FileMode mode, FileAccess access)
        {
            return File.Open(path, mode, access);
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public long Size(string filename)
        {
            var info = new FileInfo(filename);
            return info.Length;
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName,
                            bool ignoreMetadataErrors)
        {
            File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
        }
    }
}