using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nemcache.Service;
using Nemcache.Service.Commands;

namespace Nemcache.Tests.Commands
{
    [TestClass]
    public class DeleteCommandTests
    {
        [TestMethod]
        public void GivenNothing_ThenNameIsCorrect()
        {
            var command = new DeleteCommand(null);
            Assert.AreEqual("delete", command.Name);
        }

        [TestMethod]
        public void GivenNothing_WhenExecute_ThenDeleteFails()
        {
            var arrayCache = new Mock<IArrayCache>();
            var command = new DeleteCommand(arrayCache.Object);
                        var request = new Mock<IRequest>();
            request.SetupGet(r => r.Key).Returns("DeleteKey");
            var result = command.Execute(request.Object);
            Assert.AreEqual(Encoding.ASCII.GetString(result), ("NOT_FOUND\r\n"));
        }

        [TestMethod]
        public void GivenSingleDataRequest_WhenExecute_ThenDeleteSucceeds()
        {
            var arrayCache = new Mock<IArrayCache>();
            arrayCache.Setup(c => c.Get("DeleteKey")).Returns(new byte[] {1});
            var command = new DeleteCommand(arrayCache.Object);

            var request = new Mock<IRequest>();
            request.SetupGet(r => r.Key).Returns("DeleteKey");
            var result = command.Execute(request.Object);

            Assert.AreEqual(Encoding.ASCII.GetString(result), "DELETED\r\n");
            arrayCache.Verify(x => x.Remove(It.Is<string>(s => s == "DeleteKey")), Times.Once());
        }
    }
}