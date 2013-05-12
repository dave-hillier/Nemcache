using System.Collections.Generic;

namespace Nemcache.Service.IO
{
    internal class LogFileNameGenerator
    {
        private readonly string _extension;
        private readonly string _filename;

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