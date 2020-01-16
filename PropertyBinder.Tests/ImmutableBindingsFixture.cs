using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class ImmutableBindingsFixture
    {
        [Test]
        public void ShouldNotBindOnImmutablePropertyChange()
        {
            var binder = new Binder<Stub>();
            binder.Bind(x => x.Source + x.ImmutableSource).To(x => x.Target);

            var stub = new Stub();
            using (binder.Attach(stub))
            {
                stub.Target.ShouldBe(string.Empty);

                stub.Source = "1";
                stub.Target.ShouldBe("1");

                stub.ImmutableSource = "2";
                stub.Target.ShouldBe("1");

                stub.Source = "3";
                stub.Target.ShouldBe("32");
            }
        }

        [Test]
        public void ShouldNotBindOnCollectionItemImmutablePropertyChange()
        {
            var binder = new Binder<Stub>();
            binder.Bind(x => x.SubItems.All(y => string.IsNullOrEmpty(y.ImmutableSource))).To(x => x.Flag);

            var stub = new Stub();
            using (binder.Attach(stub))
            {
                stub.Flag.ShouldBe(true);

                stub.SubItems.Add(new Stub());
                stub.Flag.ShouldBe(true);

                stub.SubItems.Add(new Stub { ImmutableSource = "z" });
                stub.Flag.ShouldBe(false);

                stub.SubItems[1].ImmutableSource = null;
                stub.Flag.ShouldBe(false);

                stub.SubItems.Add(new Stub());
                stub.Flag.ShouldBe(true);
            }
        }

        private class Stub : INotifyPropertyChanged
        {
            public string Source { get; set; }

            [Immutable]
            public string ImmutableSource { get; set; }

            public string Target { get; set; }

            public bool Flag { get; set; }

            public ObservableCollection<Stub> SubItems { get; } = new ObservableCollection<Stub>();

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

}