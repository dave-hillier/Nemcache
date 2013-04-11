using System;
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
            var command = new IncrCommand(null);
            Assert.AreEqual("incr", command.Name);
        }

        [TestMethod]
        public void GivenNothing_WhenCommandIsExecuted_ThenClientError()
        {
            var arrayCache = new Mock<IArrayCache>();

            var command = new IncrCommand(arrayCache.Object);

            var mock = new Mock<IRequest>();
            mock.SetupGet(r => r.Key).Returns("MyKey");
            mock.SetupGet(r => r.Value).Returns(1);

            var response = command.Execute(mock.Object);
            throw new NotImplementedException();
        }

        [TestMethod]
        public void GivenIntValue_WhenCommandIsExecuted_ThenSuccess()
        {
            var arrayCache = new Mock<IArrayCache>();

            var command = new IncrCommand(arrayCache.Object);

            var mock = new Mock<IRequest>();
            mock.SetupGet(r => r.Key).Returns("MyKey");
            mock.SetupGet(r => r.Value).Returns(99);

            var response = command.Execute(mock.Object);
            throw new NotImplementedException();
        }

        [TestMethod]
        public void GivenNonIntValue_WhenCommandIsExecuted_ThenFail()
        {
            var arrayCache = new Mock<IArrayCache>();

            var command = new IncrCommand(arrayCache.Object);

            var mock = new Mock<IRequest>();
            mock.SetupGet(r => r.Key).Returns("MyKey");
            mock.SetupGet(r => r.Value).Returns(99);

            var response = command.Execute(mock.Object);
            throw new NotImplementedException();
        }
    }
}