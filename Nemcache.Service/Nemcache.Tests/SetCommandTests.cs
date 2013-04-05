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
            var command = new SetCommand();
            Assert.AreEqual("set", command.Name);
        }

        [TestMethod]
        public void GivenValidRequest_WhenSetExecuted_ThenResponseIsSuccess()
        {
            var command = new SetCommand();

            var mock = new Mock<IRequest>();
            mock.SetupGet(r => r.Key).Returns("MyKey");
            mock.SetupGet(r => r.Data).Returns(new byte[] { 5,6,7 });

            var response = command.Execute(mock.Object);

            Assert.AreEqual("STORED\r\n", Encoding.ASCII.GetString(response));
        }

    }
}