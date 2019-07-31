using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class TransactionFixture : BindingsFixture
    {
        [Theory]
        public void ShouldBindInTransactions(bool useDebugMode)
        {
            try
            {
                Binder.DebugMode = useDebugMode;
                _binder.Bind(x => x.String + x.Int.ToString()).To(x => x.String2);
                using (_binder.Attach(_stub))
                {
                    _stub.String2.ShouldBe("0");
                    using (_stub.VerifyChangedOnce("String2"))
                    {
                        using (Binder.BeginTransaction())
                        {
                            using (_stub.VerifyNotChanged("String2"))
                            {
                                _stub.String = "a";
                                _stub.Int = 1;
                            }

                            _stub.String2.ShouldBe("0");
                        }
                    }

                    _stub.String2.ShouldBe("a1");
                }
            }
            finally
            {
                Binder.DebugMode = false;
            }
        }
    }
}