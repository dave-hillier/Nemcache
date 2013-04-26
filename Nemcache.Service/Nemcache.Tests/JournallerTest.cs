using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class JournallerTest
    {
        private IMemCache _originalCache;
        private Journaller _journaller;
        private MemoryStream _outputStream;

        [TestMethod]
        public void TestMethod1()
        {
            _originalCache = new MemCache(1000);
            _outputStream = new MemoryStream();
            _journaller = new Journaller(_outputStream, _originalCache.Notifications);

        }
    }
}
