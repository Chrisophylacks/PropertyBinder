using System;
using System.Collections.Generic;

namespace PropertyBinder.Engine
{
    internal interface ICollectionBindingNode
    {
        bool HasBindingActions { get; }

        IBindingNode GetItemNode();

        void AddAction(int actionIndex);
    }

    internal interface ICollectionBindingNode<in TCollection> : ICollectionBindingNode
    {
        IObjectWatcher<TCollection> CreateWatcher(Func<IEnumerable<int>, Binding[]> bindingsfactory);

        ICollectionBindingNode<TCollection> Clone();

        ICollectionBindingNode<TNewCollection> CloneForDerivedParentType<TNewCollection>()
            where TNewCollection : TCollection;
    }
}