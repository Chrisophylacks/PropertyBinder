using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PropertyBinder.Engine
{
    internal sealed class ObjectWatcher<TParent, TNode> : IObjectWatcher<TParent>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly bool IsValueType;

        static ObjectWatcher()
        {
            IsValueType = typeof(TNode).IsValueType;
        }

        private readonly IDictionary<string, IObjectWatcher<TNode>> _subWatchers;
        private readonly IDictionary<string, Binding[]> _bindings;
        private readonly Func<TParent, TNode> _targetSelector;
        private TNode _target;

        public ObjectWatcher(Func<TParent, TNode> targetSelector, IDictionary<string, IObjectWatcher<TNode>> subWatchers, IDictionary<string, Binding[]> bindings)
        {
            _targetSelector = targetSelector;
            _subWatchers = subWatchers;
            _bindings = bindings;
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

            if (_subWatchers != null)
            {
                foreach (var node in _subWatchers.Values)
                {
                    node.Attach(_target);
                }
            }
        }

        public void Dispose()
        {
            Attach(default(TParent));
        }

        private void TargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IObjectWatcher<TNode> node;
            if (_subWatchers != null && _subWatchers.TryGetValue(e.PropertyName, out node))
            {
                node.Attach(_target);
            }

            Binding[] bindings;
            if (_bindings.TryGetValue(e.PropertyName, out bindings))
            {
                BindingExecutor.Execute(bindings);
            }
        }
    }
}