using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal sealed class PropertyBindingsFixture
    {
        private PropertyBinder<UniversalStub> _binder;
        private UniversalStub _stub;

        [SetUp]
        public void SetUp()
        {
            _binder = new PropertyBinder<UniversalStub>();
            _stub = new UniversalStub();
        }

        #region Simple bindings

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
            _binder.Bind(x => (x.Int * 2).ToString()).To(x => x.String2);

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

        #endregion

        #region Overrides

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

        #endregion

        #region Commands

        [Test]
        public void ShouldBindCommand()
        {
            int canExecuteCalls = 0;
            _binder.BindCommand(x => x.Int++, x => x.Flag).To(x => x.Command);

            using (_binder.Attach(_stub))
            {
                _stub.Command.ShouldNotBeNull();
                _stub.Command.CanExecute(null).ShouldBe(false);

                _stub.Command.CanExecuteChanged += (s, e) => { ++canExecuteCalls; };
                _stub.Flag = true;
                canExecuteCalls.ShouldBe(1);
                _stub.Command.CanExecute(null).ShouldBe(true);

                _stub.Int.ShouldBe(0);
                _stub.Command.Execute(null);
                _stub.Int.ShouldBe(1);
            }
        }

        [Test]
        public void ShouldOverrideCommands()
        {
            _binder.BindCommand(x => x.Int++, x => x.Flag).To(x => x.Command);
            _binder.BindCommand(x => x.Int += 10, x => x.Flag).To(x => x.Command);

            using (_binder.Attach(_stub))
            {
                _stub.Command.ShouldNotBeNull();
                _stub.Command.CanExecute(null).ShouldBe(false);

                _stub.Flag = true;
                _stub.Command.CanExecute(null).ShouldBe(true);

                _stub.Int.ShouldBe(0);
                _stub.Command.Execute(null);
                _stub.Int.ShouldBe(10);
            }
        }

        [Test]
        public void ShouldUnbindCommands()
        {
            _binder.BindCommand(x => x.Int++, x => x.Flag).To(x => x.Command);
            _binder.Unbind(x => x.Command);

            using (_binder.Attach(_stub))
            {
                _stub.Command.ShouldBe(null);
            }
        }

        #endregion

        #region Cloning

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

        #endregion

        #region Collections

        [Test]
        public void ShouldBindAggregatedCollectionOfValueTypes()
        {
            var binder = new PropertyBinder<AggregatedCollection<int>>();
            binder.Bind(x => x.Sum()).To(x => x.Aggregate);
            var collection = new AggregatedCollection<int>();

            using (binder.Attach(collection))
            {
                collection.Aggregate.ShouldBe(0);
                using (collection.VerifyChangedOnce("Aggregate"))
                {
                    collection.Add(1);
                }
                collection.Aggregate.ShouldBe(1);

                using (collection.VerifyChangedOnce("Aggregate"))
                {
                    collection.Add(2);
                }
                collection.Aggregate.ShouldBe(3);

                using (collection.VerifyChangedOnce("Aggregate"))
                {
                    collection[0] = 3;
                }
                collection.Aggregate.ShouldBe(5);

                using (collection.VerifyChangedOnce("Aggregate"))
                {
                    collection.RemoveAt(1);
                }
                collection.Aggregate.ShouldBe(3);

                using (collection.VerifyChangedOnce("Aggregate"))
                {
                    collection.Clear();
                }
                collection.Aggregate.ShouldBe(0);
            }
        }

        [Test]
        public void ShouldBindAggregatedCollection()
        {
            _binder.Bind(x => x.Collection.Sum(y => y.Int)).To(x => x.Int);
            using (_binder.Attach(_stub))
            {
                _stub.Int.ShouldBe(0);
                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Collection.Add(new UniversalStub { Int = 1 });
                }
                _stub.Int.ShouldBe(1);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Collection.Add(new UniversalStub { Int = 2 });
                }
                _stub.Int.ShouldBe(3);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Collection[0] = new UniversalStub { Int = 3 };
                }
                _stub.Int.ShouldBe(5);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Collection.RemoveAt(0);
                }
                _stub.Int.ShouldBe(2);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Collection = new ObservableCollection<UniversalStub>(new[] { new UniversalStub { Int = 4 }});
                }
                _stub.Int.ShouldBe(4);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Collection[0].Int = 5;
                }
                _stub.Int.ShouldBe(5);
            }
        }

        [Test]
        public void ShouldNotBindToTheSameCollectionItemTwice()
        {
            _binder.Bind(x => x.Collection.Sum(y => y.Int)).To(x => x.Int);
            using (_binder.Attach(_stub))
            {
                var item = new UniversalStub();
                _stub.Collection.Add(item);
                _stub.Collection.Add(item);
                _stub.Int.ShouldBe(0);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    item.Int = 1;
                }
                _stub.Int.ShouldBe(2);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    _stub.Collection.RemoveAt(1);
                }
                _stub.Int.ShouldBe(1);

                using (_stub.VerifyChangedOnce("Int"))
                {
                    item.Int = 3;
                }
                _stub.Int.ShouldBe(3);
            }
        }

        [Test]
        public void ShouldUnsubscribeFromRemovedItems()
        {
            _binder.Bind(x => string.Join(";", x.Collection.Select(s => s.String).ToArray())).To(x => x.String);
            _stub.Collection =  new ObservableCollection<UniversalStub>();

            var item1 = new UniversalStub { String = "1" };
            var item2 = new UniversalStub { String = "2" };
            _stub.Collection.Add(item1);
            _stub.Collection.Add(item2);

            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe("1;2");
                item1.SubscriptionsCount.ShouldBe(1);
                item2.SubscriptionsCount.ShouldBe(1);

                using (_stub.VerifyChangedOnce("String"))
                {
                    _stub.Collection.Remove(item1);
                }
                _stub.String.ShouldBe("2");
                item1.SubscriptionsCount.ShouldBe(0);
            }

            item2.SubscriptionsCount.ShouldBe(0);
        }

        [Test]
        public void ShouldNotSubscribeToCollectionItemsIfTheirPropertiesAreNotReferenced()
        {
            _binder.Bind(x => string.Join(";", x.Collection.Select(s => s.ToString()).ToArray())).To(x => x.String);
            _stub.Collection = new ObservableCollection<UniversalStub>();
            var item1 = new UniversalStub { String = "1" };
            _stub.Collection.Add(item1);

            using (_binder.Attach(_stub))
            {
                item1.SubscriptionsCount.ShouldBe(0);
            }
        }

        #endregion
    }
}
