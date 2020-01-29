using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PropertyBinder.Helpers
{
    /// <summary>
    /// Optimized version of System.Collections.Queue:
    /// - no enumeration support, thus no version bookkeeping
    /// - allows unsafe queue (with manual reserve)
    /// - allows dequeuing by reference
    /// - doesn't clean up elements after dequeuing (thus a potential GC prevention, should only be used in busy queues)
    /// </summary>
    /// <typeparam name="T">element name</typeparam>
    internal sealed class LiteQueue<T>
    {
        private T[] _items = new T[4];
        private int _head;
        private int _size;
        private int _capacity = 4;

        public void Enqueue(T item)
        {
            if (_size == _items.Length)
            {
                Reserve(1);
            }

            _items[(_head + _size++) % _capacity] = item;
        }

        public void EnqueueUnsafe(T item)
        {
            _items[(_head + _size++) % _capacity] = item;
        }

        public ref T DequeueRef()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException();
            }

            var cur = _head;
            _head = (_head + 1) % _capacity;
            --_size;
            return ref _items[cur];
        }

        public T Dequeue()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException();
            }

            var cur = _head;
            _head = (_head + 1) % _capacity;
            --_size;
            return _items[cur];
        }

        public int Count => _size;

        public void Reserve(int amount)
        {
            if (_size + amount > _capacity)
            {
                while (_size + amount > _capacity)
                {
                    _capacity *= 2;
                }

                var newItems = new T[_capacity];
                Array.Copy(_items, _head, newItems, 0, _items.Length - _head);
                if (_head != 0)
                {
                    Array.Copy(_items, 0, newItems, _items.Length - _head, _head);
                }

                _items = newItems;
                _head = 0;
            }
        }
    }
}