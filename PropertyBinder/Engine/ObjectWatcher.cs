using System;
using System.Collections.Generic;
using System.ComponentModel;

using PropertyBinder.Helpers;

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
        private readonly BindingMap _map;
        private TNode _target;
        private readonly PropertyChangedEventHandler _handler;
        private readonly BindingNode<TParent, TNode> _bindingNode;

        public ObjectWatcher(BindingNode<TParent, TNode> bindingNode, BindingMap map)
        {
            _bindingNode = bindingNode;
            _map = map;
            if (bindingNode.CollectionNode != null)
            {
                _collectionWatcher = bindingNode.CollectionNode.CreateWatcher(map);
            }
            _subWatchers = bindingNode.SubNodes?.ToReadOnlyDictionary2(x => x.Key, x => x.Value.CreateWatcher(map));
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

            _target = parent == null ? default(TNode) : _bindingNode.TargetSelector(parent);

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
            if (_subWatchers.TryGetValue(propertyName, out node))
            {
                node.Attach(_target);
            }

            int[] bindings;
            if (_bindingNode.BindingActions.TryGetValue(propertyName, out bindings))
            {
                BindingExecutor.Execute(_map, bindings);
            }
        }

        private void TerminalTargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            int[] bindings;
            if (_bindingNode.BindingActions.TryGetValue(e.PropertyName, out bindings))
            {
                BindingExecutor.Execute(_map, bindings);
            }
        }
    }

}