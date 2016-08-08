using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class CollectionBindingsFixture : PropertyBindingsFixture
    {
        [Test]
        public void ShouldBindAggregatedCollectionOfValueTypes()
        {
            var binder = new Binder<AggregatedCollection<int>>();
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
                    _stub.Collection = new ObservableCollection<UniversalStub>(new[] { new UniversalStub { Int = 4 } });
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
            _stub.Collection = new ObservableCollection<UniversalStub>();

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
    }
}