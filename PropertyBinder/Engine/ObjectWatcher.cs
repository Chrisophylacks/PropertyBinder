using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine
{
    internal sealed class ObjectWatcher<TContext, TParent, TNode> : IObjectWatcher<TParent>
    {
        private static readonly IDictionary<string, IObjectWatcher<TNode>> EmptyDictionary = new ReadOnlyDictionary<string, IObjectWatcher<TNode>>(new Dictionary<string, IObjectWatcher<TNode>>());

        // ReSharper disable once StaticMemberInGenericType
        private static readonly bool IsValueType;

        static ObjectWatcher()
        {
            IsValueType = typeof(TNode).IsValueType;
        }

        private readonly IDictionary<string, IObjectWatcher<TNode>> _subWatchers;
        private readonly IDictionary<string, UniqueActionCollection<TContext>> _bindingActions;
        private readonly TContext _bindingContext;
        private readonly Func<TParent, TNode> _targetSelector;
        private TNode _target;

        public ObjectWatcher(TContext bindingContext, Func<TParent, TNode> targetSelector, IDictionary<string, IObjectWatcher<TNode>> subWatchers, IDictionary<string, UniqueActionCollection<TContext>> bindingActions)
        {
            _bindingContext = bindingContext;
            _targetSelector = targetSelector;
            _subWatchers = subWatchers ?? EmptyDictionary;
            _bindingActions = bindingActions;
        }

        public void Attach(TParent parent)
        {
            if (!IsValueType)
            {
                var notify = _target as INotifyPropertyChanged;
                if (notify != null)
                {
                    notify.PropertyChanged -= TargetPropertyChanged;
                }
            }

            _target = parent == null ? default(TNode) : _targetSelector(parent);

            if (!IsValueType)
            {
                var notify = _target as INotifyPropertyChanged;
                if (notify != null)
                {
                    notify.PropertyChanged += TargetPropertyChanged;
                }
            }

            foreach (var node in _subWatchers.Values)
            {
                node.Attach(_target);
            }
        }

        public void Dispose()
        {
            Attach(default(TParent));
        }

        private void TargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IObjectWatcher<TNode> node;
            if (_subWatchers.TryGetValue(e.PropertyName, out node))
            {
                node.Attach(_target);
            }

            UniqueActionCollection<TContext> action;
            if (_bindingActions.TryGetValue(e.PropertyName, out action))
            {
                action.Execute(_bindingContext);
            }
        }
    }
}