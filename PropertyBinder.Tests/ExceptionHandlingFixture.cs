using System;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class ExceptionHandlingFixture : BindingsFixture
    {
        [Test]
        public void ShouldHandleExceptions()
        {
            Exception ex = null;
            Binder.SetExceptionHandler((_, e) =>
            {
                ex = e.Exception;
                e.Handled = true;
            });
            _binder.Bind(x => ((string)null).Trim()).To(x => x.String);
            using (_stub.VerifyNotChanged("String"))
            {
                _stub.String.ShouldBe(null);
                _binder.Attach(_stub);
                _stub.String.ShouldBe(null);
                ex.ShouldNotBe(null);
            }
        }
    }
}