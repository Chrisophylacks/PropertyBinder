using System;
using System.ComponentModel;
using Shouldly;

namespace PropertyBinder.Tests
{
    internal static class TestExtensions
    {
        public static IDisposable VerifyNotChanged(this INotifyPropertyChanged target, string propertyName)
        {
            return new Verifier(target, propertyName, 0, 0);
        }

        public static IDisposable VerifyChangedOnce(this INotifyPropertyChanged target, string propertyName)
        {
            return new Verifier(target, propertyName, 1, 1);
        }

        public static IDisposable VerifyChangedAtLeastOnce(this INotifyPropertyChanged target, string propertyName)
        {
            return new Verifier(target, propertyName, 1, Int32.MaxValue);
        }

        private sealed class Verifier : IDisposable
        {
            private readonly INotifyPropertyChanged _target;
            private readonly string _propertyName;
            private readonly int _minCount;
            private readonly int _maxCount;
            private int _count;

            public Verifier(INotifyPropertyChanged target, string propertyName, int minCount, int maxCount)
            {
                _target = target;
                _propertyName = propertyName;
                _minCount = minCount;
                _maxCount = maxCount;
                _target.PropertyChanged += Target_PropertyChanged;
            }

            private void Target_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == _propertyName)
                {
                    ++_count;
                }
            }

            public void Dispose()
            {
                _target.PropertyChanged -= Target_PropertyChanged;
                _count.ShouldBeInRange(_minCount, _maxCount, string.Format("Property '{0}' should have been notified between {1} and {2} times, but was {3}", _propertyName, _minCount, _maxCount, _count));
            }
        }
    }
}
