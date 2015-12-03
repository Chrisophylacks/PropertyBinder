using System.Collections.ObjectModel;

namespace PropertyBinder.Tests
{
    public class AggregatedCollection<T> : ObservableCollection<T>
    {
        public T Aggregate { get; set; }
    }
}