using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class ExceptionalFixture
    {
        private Binder<ExceptionalStub> _binder;
        private ExceptionalStub _stub;

        private ExceptionEventArgs ea;

        [SetUp]
        public void Setup()
        {
            _binder = new Binder<ExceptionalStub>();
            _stub = new ExceptionalStub();
        }

        [Test]
        public void ExceptionTest()
        {
            Binder.SetExceptionHandler(BinderException);

            try
            {
                _binder.Bind(x => x.F1 + x.F2).To(x => x.R);

                _stub.F1 = 1;
                _stub.F2 = 2;
                using (_binder.Attach(_stub))
                {
                    System.Diagnostics.Debug.WriteLine(ea.StampedStr);
                }
            }
            catch
            {
                ea.StampedStr.ShouldBe("R: 4;\r\nF1: 1;\r\nF2: 2;\r\n");
            }
        }

        private void BinderException(object sender, ExceptionEventArgs e)
        {
            ea = e;
        }

        internal class ExceptionalStub
        {
            public int F1 { get; set; }

            public int F2 { get; set; }

            public int R { get { return 4; } set { throw new InvalidOperationException(""); } }
        }
    }
}
