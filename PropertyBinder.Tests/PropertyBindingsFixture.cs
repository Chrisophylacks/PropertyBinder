using NUnit.Framework;

namespace PropertyBinder.Tests
{
    internal class PropertyBindingsFixture
    {
        protected Binder<UniversalStub> _binder;
        protected UniversalStub _stub;

        [SetUp]
        public void SetUp()
        {
            _binder = new Binder<UniversalStub>();
            _stub = new UniversalStub();
        }
    }
}
