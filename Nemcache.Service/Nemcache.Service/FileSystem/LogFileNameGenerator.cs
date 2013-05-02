using System.Collections.Generic;

namespace Nemcache.Service.FileSystem
{
    internal class LogFileNameGenerator
    {
        private readonly string _filename;
        private readonly string _extension;

        public LogFileNameGenerator(string filename, string extension)
        {
            _filename = filename;
            _extension = extension;
        }

        public IEnumerable<string> GetNextFileName()
        {
            int currentFile = 1;
            while (true)
                yield return string.Format("{0}.{1}.{2}", _filename, currentFile++, _extension);

        }
    }
}