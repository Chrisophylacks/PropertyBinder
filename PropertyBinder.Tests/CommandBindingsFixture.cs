using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class CommandBindingsFixture : BindingsFixture
    {
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

        [Theory]
        public void ShouldAssignCanExecuteCorrectlyWhenAttached(bool canExecute)
        {
            _binder.BindCommand(x => { }, x => x.Flag).To(x => x.Command);
            _stub.Flag = canExecute;

            using (_binder.Attach(_stub))
            {
                _stub.Command.CanExecute(null).ShouldBe(canExecute);
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

        [Test]
        public void ShouldNotCrashIfCanExecuteConditionChangesBeforeCommandIsAssigned()
        {
            _binder.Bind(x => x.Int >= 0).To(x => x.Flag);
            _binder.BindCommand(x => { }, x => x.Flag).To(x => x.Command);

            Should.NotThrow(() =>
            {
                using (_binder.Attach(_stub))
                {
                }
            });

            using (_binder.Attach(_stub))
            {
                _stub.Command.CanExecute(null).ShouldBe(true);
                _stub.Int = -1;
                _stub.Command.CanExecute(null).ShouldBe(false);
            }
        }

        [Test]
        public void ShouldAllowCustomCommandBindingDependency()
        {
            int canExecuteCalls = 0;
            _binder.BindCommand(x => { }, x=> ExternalCondition(x)).WithDependency(x => x.Flag).To(x => x.Command);

            using (_binder.Attach(_stub))
            {
                _stub.Command.CanExecuteChanged += (s, e) => { ++canExecuteCalls; };
                _stub.Command.CanExecute(null).ShouldBe(false);
                _stub.Flag = true;
                canExecuteCalls.ShouldBe(1);
                _stub.Command.CanExecute(null).ShouldBe(true);
            }
        }

        private static bool ExternalCondition(UniversalStub stub)
        {
            return stub.Flag;
        }
    }
}