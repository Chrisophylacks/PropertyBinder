using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class OverridesFixture : BindingsFixture
    {
        [Test]
        public void ShouldOverrideBindingRules()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);
            _binder.Bind(x => x.String2).To(x => x.String);

            _stub = new UniversalStub();
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe(null);
                using (_stub.VerifyNotChanged("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String2.ShouldBe(null);

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.String2 = "a";
                }
                _stub.String2.ShouldBe("a");
            }
        }

        [Test]
        public void ShouldOverrideBindingRulesByCustomKey()
        {
            _binder.Bind(x => x.Int.ToString()).OverrideKey("mykey").To(x => x.String);
            _binder.Bind(x => x.String2).OverrideKey("mykey").To(x => x.String);

            _stub = new UniversalStub();
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe(null);
                using (_stub.VerifyNotChanged("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String2.ShouldBe(null);

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.String2 = "a";
                }
                _stub.String2.ShouldBe("a");
            }
        }

        [Test]
        public void ShouldOverrideConditionalBindingRulesByCustomKey()
        {
            _binder.BindIf(x => true, x => x.Int.ToString()).OverrideKey("mykey").To((x, v) => x.String = v);
            _binder.BindIf(x => true, x => x.String2).OverrideKey("mykey").To((x, v) => x.String = v);

            _stub = new UniversalStub();
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe(null);
                using (_stub.VerifyNotChanged("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String2.ShouldBe(null);

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.String2 = "a";
                }
                _stub.String2.ShouldBe("a");
            }
        }

        [Test]
        public void ShouldNotOverrideBindingRulesIfDoNotOverrideSpecified()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);
            _binder.Bind(x => x.String2).DoNotOverride().To(x => x.String);

            _stub = new UniversalStub();
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe(null);
                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String.ShouldBe("1");

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.String2 = "a";
                }
                _stub.String.ShouldBe("a");
            }
        }

        [Test]
        public void ShouldNotOverrideBindingRulesIfKeysAreDifferent()
        {
            _binder.Bind(x => x.Int.ToString()).OverrideKey("test1").To(x => x.String);
            _binder.Bind(x => x.String2).DoNotOverride().OverrideKey("test2").To(x => x.String);

            _stub = new UniversalStub();
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe(null);
                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String.ShouldBe("1");

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.String2 = "a";
                }
                _stub.String.ShouldBe("a");
            }
        }

        [Test]
        public void ShouldOverrideMultipleRules()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);
            _binder.Bind(x => x.Flag.ToString()).To(x => x.String2);
            _binder.Bind(x => x.Int.ToString() + "2").To(x => x.String);
            _binder.Bind(x => x.Flag.ToString() + "2").To(x => x.String2);

            _stub = new UniversalStub();
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe("02");
                _stub.String2.ShouldBe("False2");
                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String.ShouldBe("12");

                using (_stub.VerifyChangedOnce("String2"))
                {
                    _stub.Flag = true;
                }
                _stub.String2.ShouldBe("True2");
            }
        }
    }
}
