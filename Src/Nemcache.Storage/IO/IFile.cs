﻿using System.IO;

namespace Nemcache.Storage.IO
{
    public interface IFile
    {
        Stream Open(string path, FileMode mode, FileAccess access);
        bool Exists(string path);
        long Size(string filename);
        void Delete(string path);
        void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors);
    }
}