using System;

namespace PropertyBinder.Engine
{
    internal interface ICollectionBindingNode<out TContext>
    {
        IBindingNode<TContext> GetItemNode();

        void AddAction(Action<TContext> action);

        void RemoveActionCascade(Action<TContext> action);
    }

    internal interface ICollectionBindingNode<TContext, in TCollection> : ICollectionBindingNode<TContext>
    {
        IObjectWatcher<TCollection> CreateWatcher(TContext context);

        ICollectionBindingNode<TNewContext, TCollection> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext;
    }
}