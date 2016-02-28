using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class NullPropagationFixture : PropertyBindingsFixture
    {
        [Test]
        public void ShouldPropagateNulls()
        {
            _binder.Bind(x => (int?)x.String.Length).PropagateNullValues().To(x => x.NullableInt);
            using (_binder.Attach(_stub))
            {
                _stub.NullableInt.ShouldBe(null);
                _stub.String = "a";
                _stub.NullableInt.ShouldBe(1);
                _stub.String = "";
                _stub.NullableInt.ShouldBe(0);
                _stub.String = null;
                _stub.NullableInt.ShouldBe(null);
            }
        }

        [Test]
        public void ShouldPropagateNullsInCoalesceOperator()
        {
            _binder.Bind(x => x.Nested.String ?? x.String).PropagateNullValues().To(x => x.String2);
            using (_binder.Attach(_stub))
            {
                _stub.String2.ShouldBe(null);
                _stub.String = "a";
                _stub.String2.ShouldBe("a");
                _stub.Nested = new UniversalStub { String = "b" };
                _stub.String2.ShouldBe("b");
            }
        }

        [Test]
        public void ShouldPropagateNullsInMethodsAndStructs()
        {
            _binder.Bind(x => x.Nested.String + x.Pair.Value.String).PropagateNullValues().To(x => x.String2);
            using (_binder.Attach(_stub))
            {
                _stub.String2.ShouldBe(string.Empty);
                _stub.Nested = new UniversalStub { String = "a" };
                _stub.String2.ShouldBe("a");
                _stub.Pair = new KeyValuePair<string, UniversalStub>(string.Empty, new UniversalStub { String = "1" });
                _stub.String2.ShouldBe("a1");
                _stub.Pair.Value.String = "2";
                _stub.String2.ShouldBe("a2");
            }
        }

        [Test]
        public void ShouldPropagateNullsWhenBindingToActions()
        {
            _binder.Bind(x => (int?)x.String.Length).PropagateNullValues().To((x, v) => x.NullableInt = v);
            using (_binder.Attach(_stub))
            {
                _stub.NullableInt.ShouldBe(null);
                _stub.String = "a";
                _stub.NullableInt.ShouldBe(1);
                _stub.String = "";
                _stub.NullableInt.ShouldBe(0);
                _stub.String = null;
                _stub.NullableInt.ShouldBe(null);
            }
        }
    }
}
