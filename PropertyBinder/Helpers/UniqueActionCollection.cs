using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PropertyBinder.Helpers
{
    /// <summary>
    /// This is a reinvention on MulticastDelegate, which has one important property - it can combine delegates of different (but argument-compatible by contravariance) types. Performance is about 50% slower than MulticastDelegate though.
    /// </summary>
    /// <typeparam name="T">Action parameter type</typeparam>
    internal sealed class UniqueActionCollection<T>
    {
        private Action<T>[] _actions;

        public UniqueActionCollection()
            : this(new Action<T>[0])
        {
        }

        private UniqueActionCollection(Action<T>[] actions)
        {
            _actions = actions;
        }

        public bool IsEmpty
        {
            get { return _actions.Length == 0; }
        }

        public IEnumerable<Action<T>> Actions
        {
            get
            {
                return _actions;
            }
        }

        public UniqueActionCollection<TDerived> Clone<TDerived>()
            where TDerived : T
        {
            var newActions = new Action<TDerived>[_actions.Length];
            _actions.CopyTo(newActions, 0);
            return new UniqueActionCollection<TDerived>(newActions);
        }
            
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Action<T> action)
        {
            if (!_actions.Contains(action, ReferenceEqualityComparer<Action<T>>.Instance))
            {
                Array.Resize(ref _actions, _actions.Length + 1);
                _actions[_actions.Length - 1] = action;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(Action<T> action)
        {
            if (_actions.Contains(action, ReferenceEqualityComparer<Action<T>>.Instance))
            {
                _actions = _actions.Except(new[] { action }).ToArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(T target)
        {
            foreach (var action in _actions)
            {
                action(target);
            }
        }
    }
}