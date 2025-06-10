using System;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Nemcache.Service;

namespace Nemcache.Tests
{
    [TestFixture]
    public class RequestConvertersTests
    {
        private RequestConverters _converters;

        [SetUp]
        public void Setup()
        {
            _converters = new RequestConverters(new TestScheduler());
        }

        [Test]
        public void ToKey_AllowsValidKey()
        {
            const string key = "valid-key";
            Assert.AreEqual(key, _converters.ToKey(key));
        }

        [Test]
        public void ToKey_RejectsControlCharacters()
        {
            Assert.Throws<InvalidOperationException>(() => _converters.ToKey("bad\nkey"));
            Assert.Throws<InvalidOperationException>(() => _converters.ToKey("bad\u007Fkey"));
        }
    }
}
