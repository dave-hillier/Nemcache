using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestClass]
    public class SetCommandTests
    {
        private const string ExpectedSuccessResponse = "STORED\r\n";

        [TestMethod]
        public void GivenNothing_ThenNameIsCorrect()
        {
            var command = new SetCommand(null);
            Assert.AreEqual("set", command.Name);
        }

        [TestMethod]
        public void GivenValidRequest_WhenSetExecuted_ThenResponseIsSuccess()
        {
            var arrayCache = new Mock<IArrayCache>();

            var command = new SetCommand(arrayCache.Object);

            var mock = new Mock<IStoreRequest>();
            mock.SetupGet(r => r.Key).Returns("MyKey");
            mock.SetupGet(r => r.Data).Returns(new byte[] { 5,6,7 });

            var response = command.Execute(mock.Object);

            Assert.AreEqual(ExpectedSuccessResponse, Encoding.ASCII.GetString(response));
        }

        [TestMethod]
        public void GivenAValidRequest_WhenSetExecuted_ThenTheCacheHasANewElementAdded()
        {
            var arrayCache = new Mock<IArrayCache>();

            var command = new SetCommand(arrayCache.Object);

            var mock = new Mock<IStoreRequest>();
            mock.SetupGet(r => r.Key).Returns("MyKey");
            var payload = new byte[] {5, 6, 7};
            mock.SetupGet(r => r.Data).Returns(payload);

            command.Execute(mock.Object);

            arrayCache.Verify(c => c.Set(It.Is<string>(k => k == "MyKey"), It.Is<byte[]>(p => p == payload)));
        }
    }
}