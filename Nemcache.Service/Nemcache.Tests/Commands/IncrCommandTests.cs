using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nemcache.Service;
using Nemcache.Service.Commands;

namespace Nemcache.Tests.Commands
{
    [TestClass]
    public class IncrCommandTests
    {
        [TestMethod]
        public void GivenNothing_ThenNameIsCorrect()
        {
            var command = new IncrCommand(null);
            Assert.AreEqual("incr", command.Name);
        }

        [TestMethod]
        public void GivenValue_WhenCommandIsExecuted_ThenCallsCache()
        {
            var arrayCache = new Mock<IArrayCache>();
            arrayCache.Setup(c => c.Increase(It.IsAny<string>(), It.IsAny<ulong>()))
                      .Returns(Encoding.ASCII.GetBytes("Value"));

            var command = new IncrCommand(arrayCache.Object);

            var mock = new Mock<IRequest>();
            mock.SetupGet(r => r.Key).Returns("MyKey");
            mock.SetupGet(r => r.Value).Returns(1);

            var response = command.Execute(mock.Object);
            var responseString = Encoding.ASCII.GetString(response);

            Assert.AreEqual("Value", responseString);
            arrayCache.Verify(c => c.Increase(It.Is<string>(k => k == "MyKey"), It.Is<ulong>(v => v == 1)));
        }
    }
}