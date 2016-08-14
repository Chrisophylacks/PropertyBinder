using System;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class IndexerBindingsFixture
    {
        [Test]
        public void ShouldBindToIndexer()
        {
            var binder = new Binder<ObservableDictionary<UniversalStub>>();
            var dict = new ObservableDictionary<UniversalStub>();

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
    }
}
