using System;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class IndexerBindingsFixture
    {
        [Test]
        public void ShouldBindBetweenIndexedObjects()
        {
            var binder = new Binder<ObservableDictionary<UniversalStub>>();

            var dict= new ObservableDictionary<UniversalStub>();

            binder.BindIf(x => x.ContainsKey("first") && x.ContainsKey("second"), x => x["first"].String).To(x => x["second"].String);

            using (binder.Attach(dict))
            {
                var first = new UniversalStub();
                var second = new UniversalStub { String = "a" };
                dict.Add("second", second);
                second.String.ShouldBe("a");
                using (second.VerifyChangedOnce("String"))
                {
                    dict.Add("first", first);
                }
                second.String.ShouldBe(null);

                using (second.VerifyChangedOnce("String"))
                {
                    first.String = "b";
                }
                second.String.ShouldBe("b");
            }
        }

        [Test]
        public void ShouldBindIndexedField()
        {
            var binder = new Binder<UniversalStub>();
            var stub = new UniversalStub();
            stub.Dictionary = new ObservableDictionary<string>();

            binder.Bind(x => x.Dictionary["test"]).To(x => x.String);

            using (binder.Attach(stub))
            {
                stub.String.ShouldBe(null);

                using (stub.VerifyChangedOnce("String"))
                {
                    stub.Dictionary.Add("test", "a");
                }

                stub.String.ShouldBe("a");
            }
        }
    }
}
