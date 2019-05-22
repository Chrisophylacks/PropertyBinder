using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class DebugFixture : BindingsFixture
    {
        [Test]
        public void ShouldInvokeDebugActionOnTriggerAndAttach()
        {
            int calls = 0;
            _binder.Bind(x => x.String).Debug(x => ++calls).To(x => x.String2);
            using (_binder.Attach(_stub))
            {
                calls.ShouldBe(1);

                _stub.String = "s";
                calls.ShouldBe(2);

                _stub.String2 = "s2";
                calls.ShouldBe(2);
            }
        }

        [Test]
        public void ShouldInvokeDebugActionOnTriggerAndAttachForConditionalBindingToProperty()
        {
            int calls = 0;
            _binder.BindIf(x => x.Int == 1, x => "a").ElseIf(x => x.Int == 2, x => "b").Debug(x => ++calls).To(x => x.String2);

            using (_binder.Attach(_stub))
            {
                calls.ShouldBe(1);

                _stub.Int = 1;
                calls.ShouldBe(2);

                _stub.Int = 2;
                calls.ShouldBe(3);

                _stub.Int = 3;
                calls.ShouldBe(4);
            }
        }

        [Test]
        public void ShouldInvokeDebugActionOnCorrectAssignmentTriggerToProperty()
        {
            int calls = 0;
            _binder.BindIf(x => x.Int == 1, x => x.String).DoNotRunOnAttach().Debug(x => ++calls).To(x => x.String2);

            using (_binder.Attach(_stub))
            {
                calls.ShouldBe(0);

                _stub.String = "a"; 
                calls.ShouldBe(0); // condition is false, don't trigger on subexpression

                _stub.Int = 1;
                calls.ShouldBe(1); // condition is true

                _stub.String = "b";
                calls.ShouldBe(2); // condition is true, subexpression trigger

                _stub.Int = 2;
                calls.ShouldBe(3); // condition true->false change trigger

                _stub.Int = 3;
                calls.ShouldBe(4); // condition false->false change trigger
            }
        }

        [Test]
        public void ShouldInvokeDebugActionOnElseClauseOnlyOnce()
        {
            int calls = 0;
            _binder.BindIf(x => x.Int == 1, x => x.String).Else(x => "a").DoNotRunOnAttach().Debug(x => ++calls).To(x => x.String2);

            using (_binder.Attach(_stub))
            {
                calls.ShouldBe(0);

                _stub.Int = 2;
                calls.ShouldBe(1);
            }
        }

        [Test]
        public void ShouldInvokeDebugActionOnCorrectAssignmentTriggerToCallback()
        {
            int calls = 0;
            int callbacks = 0;
            _binder.BindIf(x => x.Int == 1, x => x.String).DoNotRunOnAttach().Debug(x => ++calls).To((x, v) => callbacks++);

            using (_binder.Attach(_stub))
            {
                calls.ShouldBe(0);
                callbacks.ShouldBe(0);

                _stub.String = "a";
                calls.ShouldBe(0); // condition is false, don't trigger on subexpression
                callbacks.ShouldBe(0);

                _stub.Int = 1;
                calls.ShouldBe(1); // condition is true
                callbacks.ShouldBe(1);

                _stub.String = "b";
                calls.ShouldBe(2); // condition is true, subexpression trigger
                callbacks.ShouldBe(2);

                _stub.Int = 2;
                calls.ShouldBe(3); // condition true->false change trigger
                callbacks.ShouldBe(2);

                _stub.Int = 3;
                calls.ShouldBe(4); // condition false->false change trigger
                callbacks.ShouldBe(2);
            }
        }
    }
}