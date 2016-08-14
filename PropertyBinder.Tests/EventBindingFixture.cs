using NUnit.Framework;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal sealed class EventBindingFixture : BindingsFixture
    {
        [Test]
        public void ShouldBindEvent()
        {
            //_binder.BindEvent(x => x.TestEvent += x.Nested.HandleTestEvent);
        }
    }
}