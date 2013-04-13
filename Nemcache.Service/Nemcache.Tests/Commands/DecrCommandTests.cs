using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nemcache.Service;
using Nemcache.Service.Commands;

namespace Nemcache.Tests.Commands
{
    [TestClass]
    public class DecrCommandTests
    {
        [TestMethod]
        public void GivenNothing_ThenNameIsCorrect()
        {
            var command = new DecrCommand(null);
            Assert.AreEqual("decr", command.Name);
        }

        [TestMethod]
        public void GivenValue_WhenCommandIsExecuted_ThenCallsCache()
        {
            var arrayCache = new Mock<IArrayCache>();
            arrayCache.Setup(c => c.Decrease(It.IsAny<string>(), It.IsAny<ulong>()))
                      .Returns(Encoding.ASCII.GetBytes("Value"));

            var command = new DecrCommand(arrayCache.Object);

            var mock = new Mock<IRequest>();
            mock.SetupGet(r => r.Key).Returns("MyKey");
            mock.SetupGet(r => r.Value).Returns(1);

            var response = command.Execute(mock.Object);
            var responseString = Encoding.ASCII.GetString(response);

            Assert.AreEqual("Value", responseString);
            arrayCache.Verify(c => c.Decrease(It.Is<string>(k => k == "MyKey"), It.Is<ulong>(v => v == 1)));
        }
    }
}