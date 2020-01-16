using System;
using System.Collections.Generic;

namespace PropertyBinder.Engine
{
    internal sealed class CollectionBindingNode<TCollection, TItem> : ICollectionBindingNode<TCollection>
        where TCollection : IEnumerable<TItem>
    {
        private readonly List<int> _indexes;
        private IBindingNode<TItem> _itemNode;

        private CollectionBindingNode(List<int> indexes, IBindingNode<TItem> itemNode)
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

        public IBindingNode GetItemNode()
        {
            return _itemNode ?? (_itemNode = new BindingNode<TItem, TItem>(_ => _));
        }

        public IObjectWatcher<TCollection> CreateWatcher(Func<ICollection<int>, Binding[]> bindingsFactory)
        {
            return new CollectionWatcher<TCollection, TItem>(bindingsFactory(_indexes), bindingsFactory, HasBindingActions ? _itemNode : null);
        }

        public ICollectionBindingNode<TCollection> Clone()
        {
            return new CollectionBindingNode<TCollection, TItem>(new List<int>(_indexes), _itemNode != null ? _itemNode.Clone() : null);
        }

        public ICollectionBindingNode<TNewCollection> CloneForDerivedParentType<TNewCollection>()
            where TNewCollection : TCollection
        {
            return new CollectionBindingNode<TNewCollection, TItem>(new List<int>(_indexes), _itemNode != null ? _itemNode.Clone() : null);
        }
    }
}