using System;
using System.Collections.Generic;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine
{
    internal sealed class CollectionBindingNode<TContext, TCollection, TItem> : ICollectionBindingNode<TContext, TCollection>
        where TCollection : class, IEnumerable<TItem>
    {
        private IBindingNode<TContext, TItem> _itemNode;
        private Action<TContext> _bindingAction;

        private CollectionBindingNode(Action<TContext> bindingAction, IBindingNode<TContext, TItem> itemNode)
        {
            _bindingAction = bindingAction;
            _itemNode = itemNode;
        }

        public CollectionBindingNode()
            : this(null, null)
        {
        }

        public void AddAction(Action<TContext> action)
        {
            _bindingAction = _bindingAction.CombineUnique(action);
        }

        public void RemoveActionCascade(Action<TContext> action)
        {
            _bindingAction = _bindingAction.RemoveUnique(action);
        }

        public IBindingNode<TContext> GetItemNode()
        {
            if (_itemNode == null)
            {
                _itemNode = new BindingNode<TContext, TItem, TItem>(_ => _);
            }

            return _itemNode;
        }

        public IObjectWatcher<TCollection> CreateWatcher(TContext context)
        {
            return new CollectionWatcher<TContext, TCollection, TItem>(context, _bindingAction, _itemNode);
        }

        public ICollectionBindingNode<TNewContext, TCollection> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext
        {
            return new CollectionBindingNode<TNewContext, TCollection, TItem>(_bindingAction, _itemNode != null ? _itemNode.CloneForDerivedType<TNewContext>() : null);
        }
    }
}