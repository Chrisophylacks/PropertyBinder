using System;
using System.Collections.Generic;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine
{
    internal sealed class CollectionBindingNode<TContext, TCollection, TItem> : ICollectionBindingNode<TContext, TCollection>
        where TCollection : class, IEnumerable<TItem>
    {
        private readonly UniqueActionCollection<TContext> _bindingAction;
        private IBindingNode<TContext, TItem> _itemNode;

        private CollectionBindingNode(UniqueActionCollection<TContext> bindingAction, IBindingNode<TContext, TItem> itemNode)
        {
            _bindingAction = bindingAction;
            _itemNode = itemNode;
        }

        public CollectionBindingNode()
            : this(new UniqueActionCollection<TContext>(), null)
        {
        }

        public bool HasBindingActions
        {
            get { return _itemNode != null && _itemNode.HasBindingActions; }
        }

        public void AddAction(Action<TContext> action)
        {
            _bindingAction.Add(action);
        }

        public void RemoveActionCascade(Action<TContext> action)
        {
            _bindingAction.Remove(action);
        }

        public IBindingNode<TContext> GetItemNode()
        {
            return _itemNode ?? (_itemNode = new BindingNode<TContext, TItem, TItem>(_ => _));
        }

        public IObjectWatcher<TCollection> CreateWatcher(TContext context)
        {
            return new CollectionWatcher<TContext, TCollection, TItem>(context, _bindingAction, HasBindingActions ? _itemNode : null);
        }

        public ICollectionBindingNode<TNewContext, TCollection> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext
        {
            return new CollectionBindingNode<TNewContext, TCollection, TItem>(_bindingAction.Clone<TNewContext>(), _itemNode != null ? _itemNode.CloneForDerivedType<TNewContext>() : null);
        }
    }
}