using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertyBinder.Helpers
{
    internal sealed class SingleElementDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly TKey _key;
        private readonly TValue _value;

        public SingleElementDictionary(TKey key, TValue value)
        {
            _key = key;
            _value = value;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new List<KeyValuePair<TKey, TValue>>(1) { new KeyValuePair<TKey, TValue>(_key, _value) }.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => 1;

        public bool ContainsKey(TKey key)
        {
            return _key.Equals(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_key.Equals(key))
            {
                value = _value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (!_key.Equals(key))
                {
                    throw new IndexOutOfRangeException();
                }

                return _value;
            }
        }

        public IEnumerable<TKey> Keys
        {
            get { yield return _key; }
        }

        public IEnumerable<TValue> Values
        {
            get { yield return _value; }
        }
    }
}
