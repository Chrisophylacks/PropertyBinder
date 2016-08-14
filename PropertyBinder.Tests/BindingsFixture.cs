using NUnit.Framework;

namespace PropertyBinder.Tests
{
    internal class BindingsFixture
    {
        //protected PropertyBinder<UniversalStub> _binder;
        protected Binder<UniversalStub> _binder;
        protected UniversalStub _stub;

        [SetUp]
        public void SetUp()
        {
            //_binder = new PropertyBinder<UniversalStub>();
            _binder = new Binder<UniversalStub>();
            _stub = new UniversalStub();
        }
    }
}
