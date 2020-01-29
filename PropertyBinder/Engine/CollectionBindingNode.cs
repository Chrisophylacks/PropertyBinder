using System;
using System.Collections.Generic;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine
{
    internal sealed class CollectionBindingNodeBuilder<TCollection, TItem> : ICollectionBindingNodeBuilder<TCollection>
        where TCollection : IEnumerable<TItem>
    {
        private readonly List<int> _indexes;
        private IBindingNodeBuilder<TItem> _itemNode;

        private CollectionBindingNodeBuilder(List<int> indexes, IBindingNodeBuilder<TItem> itemNode)
        {
            _indexes = indexes;
            _itemNode = itemNode;
        }

        public CollectionBindingNodeBuilder()
            : this(new List<int>(), null)
        {
        }

        public bool HasBindingActions => _indexes.Count > 0 || (_itemNode != null && _itemNode.HasBindingActions);

        public void AddAction(int index)
        {
            _indexes.Add(index);
        }

        public IBindingNodeBuilder GetItemNode()
        {
            return _itemNode ?? (_itemNode = new BindingNodeBuilder<TItem, TItem>(_ => _));
        }

        public ICollectionBindingNodeBuilder<TCollection> Clone()
        {
            return new CollectionBindingNodeBuilder<TCollection, TItem>(new List<int>(_indexes), _itemNode?.Clone());
        }

        public ICollectionBindingNodeBuilder<TNewCollection> CloneForDerivedParentType<TNewCollection>()
            where TNewCollection : TCollection
        {
            return new CollectionBindingNodeBuilder<TNewCollection, TItem>(new List<int>(_indexes), _itemNode?.Clone());
        }

        public ICollectionBindingNode<TCollection> CreateBindingNode(int[] actionRemap)
        {
            return new CollectionBindingNode<TCollection, TItem>(_indexes.CompactRemap(actionRemap), _itemNode != null && _itemNode.HasBindingActions ? _itemNode.CreateBindingNode(actionRemap) : null);
        }
    }

    internal sealed class CollectionBindingNode<TCollection, TItem> : ICollectionBindingNode<TCollection>
        where TCollection : IEnumerable<TItem>
    {
        public readonly int[] Indexes;
        public readonly IBindingNode<TItem> ItemNode;

        public CollectionBindingNode(int[] indexes, IBindingNode<TItem> itemNode)
        {
            Indexes = indexes;
            ItemNode = itemNode;
        }

        public IObjectWatcher<TCollection> CreateWatcher(BindingMap map)
        {
            return new CollectionWatcher<TCollection, TItem>(this, map);
        }
    }
}