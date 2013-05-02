using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nemcache.Tests
{
    [TestClass]
    class CompacterTests
    {
        [TestMethod]
        public void Test()
        {
            // Open an existing log.
            // Start writing to another log. 
            // Remove the old log - or anything that is not current?
            // After a preset amount written or a time period repeat the process.
            // In an atomic fashion
        }
    }
}
