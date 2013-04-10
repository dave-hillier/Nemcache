using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nemcache.Service;
using Nemcache.Service.Commands;

namespace Nemcache.Tests.Commands
{
    //VALUE <key> <flags> <bytes> [<cas unique>]\r\n
    //<data block>\r\n

    [TestClass]
    public class GetCommandTests
    {
        [TestMethod]
        public void GivenNothing_ThenNameIsCorrect()
        {
            var command = new GetCommand(null);
            Assert.AreEqual("get", command.Name);
        }

        [TestMethod]
        public void GivenSingleDataRequest_WhenExecute_ThenSingleResult()
        {
            var arrayCache = new Mock<IArrayCache>();
            arrayCache.Setup(c => c.Get(It.Is<string>(k => k == "ABCD"))).
                Returns(Encoding.ASCII.GetBytes("<data block>"));

            var request = new Mock<IRequest>();
            request.SetupGet(r => r.Key).Returns("ABCD");
            var command = new GetCommand(arrayCache.Object);

            var result = command.Execute(request.Object);
            var asString = Encoding.ASCII.GetString(result);

            Assert.AreEqual("VALUE ABCD 0 12\r\n<data block>\r\nEND\r\n", asString);
        }

        [TestMethod]
        public void GivenNothing_WhenExecute_ThenFailed()
        {
            var arrayCache = new Mock<IArrayCache>();

            var request = new Mock<IRequest>();
            request.SetupGet(r => r.Key).Returns("ABCD");
            var command = new GetCommand(arrayCache.Object);

            var result = command.Execute(request.Object);
            var asString = Encoding.ASCII.GetString(result);

            Assert.AreEqual("END\r\n", asString);
        }
        
        // TODO: multi get
        // TODO: name
    }
}   