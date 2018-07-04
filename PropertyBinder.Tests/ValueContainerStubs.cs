using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PropertyBinder.Tests
{
    public class ValueContainerClass<T>
    {
        public T Value { get; set; }
    }

    public class ValueContainerClassNotify<T> : INotifyPropertyChanged
    {
        public T Value { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public struct ValueContainerStruct<T>
    {
        public ValueContainerStruct(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}