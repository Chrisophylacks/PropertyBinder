using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class CloneBindingsFixture : PropertyBindingsFixture
    {
        [Test]
        public void ClonedBindersShouldInheritBindings()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);

            var binder2 = _binder.Clone<UniversalStub>();
            using (binder2.Attach(_stub))
            {
                _stub.String.ShouldBe("0");

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String.ShouldBe("1");
            }
        }

        [Test]
        public void ModifiedClonedBindersShouldNotAffectOriginalOnes()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);

            var binder2 = _binder.Clone<UniversalStub>();
            binder2.Bind(x => x.Flag.ToString()).To(x => x.String2);

            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe("0");
                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String.ShouldBe("1");

                _stub.String2.ShouldBe(null);
                using (_stub.VerifyNotChanged("String2"))
                {
                    _stub.Flag = true;
                }
                _stub.String2.ShouldBe(null);
            }

            _stub = new UniversalStub();
            using (binder2.Attach(_stub))
            {
                _stub.String.ShouldBe("0");
                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String.ShouldBe("1");

                _stub.String2.ShouldBe("False");
                using (_stub.VerifyChangedOnce("String2"))
                {
                    _stub.Flag = true;
                }
                _stub.String2.ShouldBe("True");
            }
        }

        [Test]
        public void ClonedBindersShouldCorrectlyMixDerivedActionsWithBaseOnes()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);

            var binder2 = _binder.Clone<UniversalStubEx>();
            binder2.Bind(x => x.Int.ToString()).To(x => x.String3);

            var stub = new UniversalStubEx();

            using (binder2.Attach(stub))
            {
                stub.String.ShouldBe("0");
                stub.String3.ShouldBe("0");

                using (stub.VerifyChangedOnce("String"))
                using (stub.VerifyChangedOnce("String3"))
                {
                    stub.Int = 1;
                }

                stub.String.ShouldBe("1");
                stub.String3.ShouldBe("1");
            }
        }

        [Test]
        public void ShouldBindNestedMembersOfDerivedType()
        {
            var binderEx = _binder.Clone<UniversalStubEx>();
            binderEx.Bind(x => x.NestedEx.Int).To(x => x.Int);

            var stub = new UniversalStubEx { NestedEx = new UniversalStubEx { Int = 1 } };

            using (binderEx.Attach(stub))
            {
                stub.Int.ShouldBe(1);

                using (stub.VerifyChangedOnce("Int"))
                {
                    stub.NestedEx.Int = 2;
                }
                stub.Int.ShouldBe(2);
            }
        }
    }
}