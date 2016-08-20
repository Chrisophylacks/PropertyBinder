using System;
using System.Collections.Generic;
using System.Linq;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine
{
    internal sealed class CollectionBindingNode<TContext, TCollection, TItem> : ICollectionBindingNode<TContext, TCollection>
        where TCollection : class, IEnumerable<TItem>
    {
        private readonly List<int> _indexes;
        private IBindingNode<TContext, TItem> _itemNode;

        private CollectionBindingNode(List<int> indexes, IBindingNode<TContext, TItem> itemNode)
        {
            _indexes = indexes;
            _itemNode = itemNode;
        }

        public CollectionBindingNode()
            : this(new List<int>(), null)
        {
        }

        public bool HasBindingActions
        {
            get { return _itemNode != null && _itemNode.HasBindingActions; }
        }

        public void AddAction(int index)
        {
            _indexes.Add(index);
        }

        public IBindingNode<TContext> GetItemNode()
        {
            return _itemNode ?? (_itemNode = new BindingNode<TContext, TItem, TItem>(_ => _));
        }

        public IObjectWatcher<TCollection> CreateWatcher(Func<IEnumerable<int>, Binding[]> bindingsFactory)
        {
            return new CollectionWatcher<TContext, TCollection, TItem>(bindingsFactory(_indexes), bindingsFactory, HasBindingActions ? _itemNode : null);
        }

        public ICollectionBindingNode<TNewContext, TCollection> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext
        {
            return new CollectionBindingNode<TNewContext, TCollection, TItem>(new List<int>(_indexes), _itemNode != null ? _itemNode.CloneForDerivedType<TNewContext>() : null);
        }
    }
}