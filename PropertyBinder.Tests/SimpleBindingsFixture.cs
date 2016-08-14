using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class SimpleBindingsFixture : BindingsFixture
    {
        [Test]
        public void ShouldAssignBoundPropertyWhenAttached()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);
            using (_stub.VerifyChangedOnce("String"))
            {
                _stub.String.ShouldBe(null);
                _binder.Attach(_stub);
                _stub.String.ShouldBe("0");
            }
        }

        [Test]
        public void ShouldBindPropertyWhileAttached()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe("0");
                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String.ShouldBe("1");
            }

            using (_stub.VerifyNotChanged("String"))
            {
                _stub.Int = 2;
            }

            _stub.String.ShouldBe("1");
        }

        [Test]
        public void ShouldBindConditionalExpressions()
        {
            _binder.Bind(x => x.Flag ? x.Int.ToString() : "empty").To(x => x.String);
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe("empty");

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Flag = true;
                }
                _stub.String.ShouldBe("0");

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Int = 1;
                }
                _stub.String.ShouldBe("1");

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Flag = false;
                }
                _stub.String.ShouldBe("empty");
            }
        }

        [Test]
        public void ShouldNotAssignBoundPropertyWhenAttachedIfDoNotRunOnAttachSpecified()
        {
            _binder.Bind(x => x.Int.ToString()).DoNotRunOnAttach().To(x => x.String);

            using (_stub.VerifyNotChanged("String"))
            {
                _binder.Attach(_stub);
            }

            _stub.String.ShouldBe(null);
            using (_stub.VerifyChangedOnce("String"))
            {
                _stub.Int = 1;
            }
            _stub.String.ShouldBe("1");
        }

        [Test]
        public void ShouldBindNestedProperties()
        {
            _binder.Bind(x => x.Nested != null ? x.Nested.Int : 0).To(x => x.Int);

            using (_binder.Attach(_stub))
            {
                _stub.Int.ShouldBe(0);
                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Nested = new UniversalStub { Int = 1 };
                }
                _stub.Int.ShouldBe(1);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Nested.Int = 2;
                }
                _stub.Int.ShouldBe(2);
            }
        }

        [Test]
        public void ShouldBindToNestedProperties()
        {
            _binder.Bind(x => x.Int).To(x => x.Nested.Int);
            _stub.Nested = new UniversalStub();

            using (_binder.Attach(_stub))
            {
                _stub.Nested.Int.ShouldBe(0);
                using (_stub.Nested.VerifyChangedOnce("Int"))
                {
                    _stub.Int = 1;
                }
                _stub.Nested.Int.ShouldBe(1);

                _stub.Nested = new UniversalStub();
                _stub.Nested.Int.ShouldBe(1);
            }
        }

        [Test]
        public void ShouldBindStructProperties()
        {
            _binder.Bind(x => x.DateTime.Year).To(x => x.Int);

            using (_binder.Attach(_stub))
            {
                _stub.Int.ShouldBe(default(DateTime).Year);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.DateTime = new DateTime(2000, 1, 1);
                }
                _stub.Int.ShouldBe(2000);
            }
        }

        [Test]
        public void ShouldBindNestedStructProperties()
        {
            _binder.Bind(x => x.Pair.Value.String).To(x => x.String);
            _stub.Pair = new KeyValuePair<string, UniversalStub>(string.Empty, new UniversalStub());

            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe(null);

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Pair.Value.String = "a";
                }
                _stub.String.ShouldBe("a");

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Pair = new KeyValuePair<string, UniversalStub>(string.Empty, new UniversalStub { String = "b" });
                }
                _stub.String.ShouldBe("b");

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Pair.Value.String = "c";
                }
                _stub.String.ShouldBe("c");
            }
        }

        [Test]
        public void ShouldBindPropertyToField()
        {
            _binder.Bind(x => x.String).To(x => x.StringField);

            using (_binder.Attach(_stub))
            {
                _stub.StringField.ShouldBe(null);
                _stub.String = "a";
                _stub.StringField.ShouldBe("a");
            }
        }

        [Test]
        public void ShouldBindFieldToProperty()
        {
            _binder.Bind(x => x.StringField).To(x => x.String);

            _stub.StringField = "a";
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe("a");
            }
        }

        [Test]
        public void ShouldSubscribeOnlyOncePerSource()
        {
            _binder.Bind(x => x.Nested.Int.ToString() + x.Nested.String).To(x => x.String);
            _binder.Bind(x => x.Nested.Flag).To(x => x.Flag);

            _stub.Nested = new UniversalStub();

            using (_binder.Attach(_stub))
            {
                _stub.Nested.SubscriptionsCount.ShouldBe(1);
                _stub.String.ShouldBe("0");
                _stub.Flag.ShouldBe(false);

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Nested.Int = 1;
                }
                _stub.String.ShouldBe("1");

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Nested.String = "a";
                }
                _stub.String.ShouldBe("1a");

                using (_stub.VerifyChangedOnce("Flag"))
                {
                    _stub.Nested.Flag = true;
                }
                _stub.Flag.ShouldBe(true);
            }
        }

        [Test]
        public void ShouldBindOnlyOncePerExpression()
        {
            _binder.Bind(x => x.Nested.Int + x.Nested.Int).To(x => x.Int);
            _stub.Nested = new UniversalStub();

            using (_binder.Attach(_stub))
            {
                _stub.Int.ShouldBe(0);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Nested.Int = 1;
                }
                _stub.Int.ShouldBe(2);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Nested = new UniversalStub { Int = 2 };
                }
                _stub.Int.ShouldBe(4);
            }
        }

        [Test]
        public void ShouldBindMultipleRulesForProperty()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);
            _binder.Bind(x => (x.Int * 2).ToString()).Debug(x => { }).To(x => x.String2);

            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe("0");
                _stub.String2.ShouldBe("0");

                using (_stub.VerifyChangedOnce("String"))
                using (_stub.VerifyChangedOnce("String2"))
                {
                    _stub.Int = 1;
                }

                _stub.String.ShouldBe("1");
                _stub.String2.ShouldBe("2");
            }
        }

        [Test]
        public void ShouldBindToMethod()
        {
            _binder.Bind(x => x.String).To(x => x.Int++);

            _stub.String = "a";
            using (_binder.Attach(_stub))
            {
                _stub.Int.ShouldBe(1);
                _stub.String = "b";
                _stub.Int.ShouldBe(2);
            }
        }

        [Test]
        public void ShouldNotCrashIfAppliedWithoutRules()
        {
            Should.NotThrow(() =>
            {
                using (_binder.Attach(_stub))
                {
                }
            });
        }

        [Test]
        public void ShouldNotCrashWhenBindingStaticMembers()
        {
            _binder.Bind(x => Environment.ProcessorCount).To(x => x.Int);
            _binder.Bind(x => String.Empty).To(x => x.String);
            using (_binder.Attach(_stub))
            {
                _stub.Int.ShouldBe(Environment.ProcessorCount);
                _stub.String.ShouldBe(string.Empty);
            }
        }

        [Test]
        public void ShouldInitializeCorrectlyWhenChainBindingOrderIsReversed()
        {
            _binder.Bind(x => x.String).To(x => x.String2);
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);

            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe("0");
                _stub.String2.ShouldBe("0");
            }
        }

        [Test]
        public void ShouldSupportDebugging()
        {
            string lastStringValue = null;
            _binder.Bind(x => x.String)
                .Debug(x => { lastStringValue = x.String; })
                .To(x => x.StringField);

            _stub.String = "a";
            using (_binder.Attach(_stub))
            {
                lastStringValue.ShouldBe("a");
                _stub.StringField.ShouldBe("a");
                _stub.String = "b";
                lastStringValue.ShouldBe("b");
                _stub.StringField.ShouldBe("b");
            }
        }
    }
}