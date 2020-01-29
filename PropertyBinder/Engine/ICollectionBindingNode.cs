using System;
using System.Collections.Generic;

namespace PropertyBinder.Engine
{
    internal interface ICollectionBindingNodeBuilder
    {
        bool HasBindingActions { get; }

        IBindingNodeBuilder GetItemNode();

        void AddAction(int actionIndex);
    }

    internal interface ICollectionBindingNodeBuilder<in TCollection> : ICollectionBindingNodeBuilder
    {
        ICollectionBindingNodeBuilder<TCollection> Clone();

        ICollectionBindingNodeBuilder<TNewCollection> CloneForDerivedParentType<TNewCollection>()
            where TNewCollection : TCollection;

        ICollectionBindingNode<TCollection> CreateBindingNode(int[] actionRemap);
    }

    internal interface ICollectionBindingNode<in TCollection>
    {
        IObjectWatcher<TCollection> CreateWatcher(BindingMap map);
    }
}