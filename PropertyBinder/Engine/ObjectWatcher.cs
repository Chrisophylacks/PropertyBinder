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

        private readonly IReadOnlyDictionary<string, IObjectWatcher<TNode>> _subWatchers;
        private readonly IObjectWatcher<TNode> _collectionWatcher;
        private readonly IReadOnlyDictionary<string, Binding[]> _bindings;
        private readonly Func<TParent, TNode> _targetSelector;
        private TNode _target;
        private readonly PropertyChangedEventHandler _handler;

        public ObjectWatcher(Func<TParent, TNode> targetSelector, IReadOnlyDictionary<string, IObjectWatcher<TNode>> subWatchers, IReadOnlyDictionary<string, Binding[]> bindings, IObjectWatcher<TNode> collectionWatcher)
        {
            _targetSelector = targetSelector;
            _subWatchers = subWatchers;
            _bindings = bindings;
            _collectionWatcher = collectionWatcher;
            _handler = _subWatchers == null ? TerminalTargetPropertyChanged : new PropertyChangedEventHandler(TargetPropertyChanged);
        }

        public void Attach(TParent parent)
        {
            if (!IsValueType)
            {
                var notify = _target as INotifyPropertyChanged;
                if (notify != null)
                {
                    notify.PropertyChanged -= _handler;
                }
            }

            _target = parent == null ? default(TNode) : _targetSelector(parent);

            if (!IsValueType)
            {
                var notify = _target as INotifyPropertyChanged;
                if (notify != null)
                {
                    notify.PropertyChanged += _handler;
                }
            }

            if (_subWatchers != null)
            {
                foreach (var node in _subWatchers.Values)
                {
                    node.Attach(_target);
                }
            }

            _collectionWatcher?.Attach(_target);
        }

        public void Dispose()
        {
            Attach(default(TParent));
        }

        private void TargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IObjectWatcher<TNode> node;
            var propertyName = e.PropertyName;
            if (_subWatchers.TryGetValue(e.PropertyName, out node))
            {
                node.Attach(_target);
            }

            Binding[] bindings;
            if (_bindings.TryGetValue(propertyName, out bindings))
            {
                BindingExecutor.Execute(bindings);
            }
        }

        private void TerminalTargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Binding[] bindings;
            if (_bindings.TryGetValue(e.PropertyName, out bindings))
            {
                BindingExecutor.Execute(bindings);
            }
        }
    }
}