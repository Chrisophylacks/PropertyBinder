using System;
using System.Collections.Generic;

namespace PropertyBinder.Engine
{
    internal interface ICollectionBindingNode<out TContext>
    {
        bool HasBindingActions { get; }

        IBindingNode<TContext> GetItemNode();

        void AddAction(int actionIndex);
    }

    internal interface ICollectionBindingNode<TContext, in TCollection> : ICollectionBindingNode<TContext>
    {
        IObjectWatcher<TCollection> CreateWatcher(Func<IEnumerable<int>, Binding[]> bindingsfactory);

        ICollectionBindingNode<TNewContext, TCollection> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext;
    }
}