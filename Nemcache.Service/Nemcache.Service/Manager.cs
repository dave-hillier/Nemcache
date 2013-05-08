using System.IO;
using Nemcache.Service;
using Nemcache.Service.IO;

internal static class Manager
{
    private static void RestoreFromLog(MemCache memCache, uint partitionLength, FileSystemWrapper fileSystemWrapper,
                                       string fileNameWithoutExtension, string extension)
    {
        if (fileSystemWrapper.File.Exists(fileNameWithoutExtension + ".0." + extension))
        {
            using (var existingLog = new PartitioningFileStream(
                fileSystemWrapper, fileNameWithoutExtension, extension, partitionLength, FileAccess.Read))
            {
                StreamArchiver.Restore(existingLog, memCache);
            }
        }
    }
}