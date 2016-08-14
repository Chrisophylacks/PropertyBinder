using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class ConditionalBindingsFixture : BindingsFixture
    {
        [Test]
        public void ShouldBindConditionally()
        {
            _binder.BindIf(x => x.Int == 1, x => x.String + "1")
                .ElseIf(x => x.Int > 0, x => x.Flag.ToString())
                .Else(x => x.String + "2")
                .To(x => x.String2);

            using (_binder.Attach(_stub))
            {
                _stub.String2.ShouldBe("2");

                using (_stub.VerifyChangedOnce("String2"))
                {
                    _stub.String = "a";
                }
                _stub.String2.ShouldBe("a2");

                using (_stub.VerifyChangedOnce("String2"))
                {
                    _stub.Int = 2;
                }
                _stub.String2.ShouldBe("False");

                using (_stub.VerifyChangedOnce("String2"))
                {
                    _stub.Flag = true;
                }
                _stub.String2.ShouldBe("True");

                using (_stub.VerifyChangedOnce("String2"))
                {
                    _stub.Int = 1;
                }
                _stub.String2.ShouldBe("a1");

                using (_stub.VerifyChangedOnce("String2"))
                {
                    _stub.String = "b";
                }
                _stub.String2.ShouldBe("b1");
            }
        }

        [Test]
        public void ShouldBindConditionallyWithoutElseClause()
        {
            _binder.BindIf(x => x.Int == 0, x => x.String + "1")
                .ElseIf(x => x.Int == 1, x => x.String + "2")
                .To(x => x.String2);

            using (_binder.Attach(_stub))
            {
                _stub.String2.ShouldBe("1");

                using (_stub.VerifyChangedAtLeastOnce("String2"))
                {
                    _stub.String = "a";
                }
                _stub.String2.ShouldBe("a1");

                using (_stub.VerifyChangedAtLeastOnce("String2"))
                {
                    _stub.Int = 1;
                }
                _stub.String2.ShouldBe("a2");

                using (_stub.VerifyChangedAtLeastOnce("String2"))
                {
                    _stub.String = "b";
                }
                _stub.String2.ShouldBe("b2");

                using (_stub.VerifyNotChanged("String2"))
                {
                    _stub.Int = 2;
                    _stub.String = "c";
                }
                _stub.String2.ShouldBe("b2");
            }
        }

        [Test]
        public void WhenBindingConditionallyInactiveBranchesShouldNotAssignProperties()
        {
            _binder.BindIf(x => x.Int == 1, x => x.String + "1")
                .Else(x => x.Flag.ToString())
                .To(x => x.String2);

            using (_binder.Attach(_stub))
            {
                _stub.String2.ShouldBe("False");

                _stub.String2 = "override";
                using (_stub.VerifyNotChanged("String2"))
                {
                    _stub.String = "a";
                }
                _stub.String2.ShouldBe("override");

                using (_stub.VerifyChangedOnce("String2"))
                {
                    _stub.Int = 1;
                }
                _stub.String2.ShouldBe("a1");
            }
        }

        [Test]
        public void WhenBindingConditionallyInactiveBranchesShouldNotInvokeActions()
        {
            int callCount = 0;
            string lastValue = null;

            _binder.BindIf(x => x.Int == 1, x => x.String + "1")
                .Else(x => x.Flag.ToString())
                .To((x, v) =>
                {
                    ++callCount;
                    lastValue = v;
                });

            using (_binder.Attach(_stub))
            {
                callCount.ShouldBe(1);
                lastValue.ShouldBe("False");

                _stub.String = "a";
                callCount.ShouldBe(1);
                lastValue.ShouldBe("False");

                _stub.Int = 1;
                callCount.ShouldBe(2);
                lastValue.ShouldBe("a1");
            }
        }

        [Test]
        public void ShouldOverrideConditionalBindings()
        {
            _binder.BindIf(x => x.Flag, x => x.Int.ToString())
                .Else(x => x.Int.ToString() + "1")
                .To(x => x.String);

            var binder = _binder.Clone<UniversalStubEx>();
            binder.Bind(x => x.String2).To(x => x.String);
            var stub = new UniversalStubEx();

            using (binder.Attach(stub))
            {
                stub.String = null;

                using (stub.VerifyNotChanged("String"))
                {
                    stub.Flag = !stub.Flag;
                }

                using (stub.VerifyNotChanged("String"))
                {
                    stub.Int = 1;
                }

                using (stub.VerifyChangedOnce("String"))
                {
                    stub.String2 = "a";
                }
                stub.String = "a";
            }
        }

        [Test]
        public void ShouldNotOverrideConditionalBindingsIfDoNotOverrideSpecified()
        {
            _binder.BindIf(x => x.Flag, x => x.Int.ToString())
                .Else(x => x.Int.ToString() + "1")
                .To(x => x.String);

            var binder = _binder.Clone<UniversalStubEx>();
            binder.Bind(x => x.String2).DoNotOverride().To(x => x.String);
            var stub = new UniversalStubEx();

            using (binder.Attach(stub))
            {
                stub.String = null;

                using (stub.VerifyChangedOnce("String"))
                {
                    stub.Int = 1;
                }
                stub.String = "11";

                using (stub.VerifyChangedOnce("String"))
                {
                    stub.String2 = "a";
                }
                stub.String = "a";
            }
        }

        [Test]
        public void ShouldNotOverrideConditionalBindingsByAnotherConditionalIfDoNotOverrideSpecified()
        {
            _binder.BindIf(x => x.Flag, x => x.Int.ToString())
                .Else(x => x.Int.ToString() + "1")
                .To(x => x.String);

            var binder = _binder.Clone<UniversalStubEx>();
            binder.BindIf(x => true, x => x.String2).DoNotOverride().To(x => x.String);
            var stub = new UniversalStubEx();

            using (binder.Attach(stub))
            {
                stub.String = null;

                using (stub.VerifyChangedOnce("String"))
                {
                    stub.Int = 1;
                }
                stub.String = "11";

                using (stub.VerifyChangedOnce("String"))
                {
                    stub.String2 = "a";
                }
                stub.String = "a";
            }
        }
    }
}