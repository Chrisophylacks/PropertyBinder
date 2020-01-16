using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PropertyBinder.Experiments
{
    public sealed class ListDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private KeyValuePair<TKey, TValue>[] data;
        private readonly IEqualityComparer<TKey> keyComparer;
        private int size;

        public ListDictionary()
            : this(4, EqualityComparer<TKey>.Default)
        {
        }

        public ListDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            keyComparer = comparer;
            data = new KeyValuePair<TKey, TValue>[capacity];
        }

        public bool ContainsKey(TKey key)
        {
            for (int i = 0; i < size; ++i)
            {
                if (keyComparer.Equals(data[i].Key, key))
                {
                    return true;
                }
            }
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            if (size >= data.Length)
            {
                Array.Resize(ref data, data.Length * 2);
            }

            data[size++] = new KeyValuePair<TKey, TValue>(key, value);
        }

        public bool Remove(TKey key)
        {
            for (int i = 0; i < size;++i)
            {
                if (keyComparer.Equals(data[i].Key, key))
                {
                    data[i] = data[--size];
                    data[size] = default (KeyValuePair<TKey, TValue>);
                    return true;
                }
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            for (int i = 0; i < size; ++i)
            {
                if (keyComparer.Equals(data[i].Key, key))
                {
                    value = data[i].Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue res;
                if (!TryGetValue(key, out res))
                {
                    throw new KeyNotFoundException();
                }
                return res;
            }
            set
            {
                for (int i = 0; i < size; ++i)
                {
                    if (keyComparer.Equals(data[i].Key, key))
                    {
                        data[i] = new KeyValuePair<TKey, TValue>(key, value);
                        return;
                    }
                }
                Add(key, value);
            }
        }


        public ICollection<TKey> Keys
        {
            get
            {
               throw new NotImplementedException(); 
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Array.Clear(data, 0, size);
            size = 0;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Array.Copy(data, 0, array, arrayIndex, size);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public int Count => size;

        public bool IsReadOnly => false;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return data.Take(size).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
