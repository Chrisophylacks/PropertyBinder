using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PropertyBinder.Tests
{
    internal sealed class UniversalStub : INotifyPropertyChanged
    {
        public UniversalStub()
        {
            Collection = new ObservableCollection<UniversalStub>();
        }

        public string StringField;

        public UniversalStub StubField;

        public int Int { get; set; }

        public bool Flag { get; set; }

        public string String { get; set; }

        public string String2 { get; set; }

        public DateTime DateTime { get; set; }

        public UniversalStub Nested { get; set; }

        public KeyValuePair<string, UniversalStub> Pair { get; set; }

        public ICommand Command { get; set; }

        public ObservableCollection<UniversalStub> Collection { get; set; }

        public int SubscriptionsCount
        {
            get
            {
                if (PropertyChanged == null)
                {
                    return 0;
                }

                return PropertyChanged.GetInvocationList().Length;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
