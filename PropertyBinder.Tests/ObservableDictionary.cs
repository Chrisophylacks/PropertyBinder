using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PropertyBinder.Tests
{
    internal sealed class ObservableDictionary<TValue> : INotifyPropertyChanged
    {
        private readonly Dictionary<string, TValue> _dictionary = new Dictionary<string, TValue>();

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

        public void Add(string key, TValue value)
        {
            _dictionary.Add(key, value);
            OnPropertyChanged(key);
        }

        public bool Remove(string key)
        {
            if (_dictionary.Remove(key))
            {
                OnPropertyChanged(key);
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out TValue value) => _dictionary.TryGetValue(key, out value);

        public TValue this[string key]
        {
            get
            {
                TValue res;
                TryGetValue(key, out res);
                return res;
            }
            set
            {
                _dictionary[key] = value;
                OnPropertyChanged(key);
            }
        }

        public ICollection<string> Keys => _dictionary.Keys;

        public ICollection<TValue> Values => _dictionary.Values;

        private void OnPropertyChanged(string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}