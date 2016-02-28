using NUnit.Framework;

namespace PropertyBinder.Tests
{
    internal class PropertyBindingsFixture
    {
        protected PropertyBinder<UniversalStub> _binder;
        protected UniversalStub _stub;

        [SetUp]
        public void SetUp()
        {
            _binder = new PropertyBinder<UniversalStub>();
            _stub = new UniversalStub();
        }
    }
}
