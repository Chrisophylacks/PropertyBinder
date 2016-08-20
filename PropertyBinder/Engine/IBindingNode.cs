using System;
using System.Collections.Generic;

namespace PropertyBinder.Engine
{
    internal interface IBindingNode<out TContext>
    {
        bool HasBindingActions { get; }

        IBindingNode<TContext> GetSubNode(BindableMember member);

        ICollectionBindingNode<TContext> GetCollectionNode(Type itemType);

        void AddAction(string propertyName, int actionIndex);
    }

    internal interface IBindingNode<TContext, in TParent> : IBindingNode<TContext>
    {
        IBindingNode<TNewContext, TParent> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext;

        IBindingNode<TNewContext, TNewContext> CloneSubRootForDerivedType<TNewContext>()
            where TNewContext : class, TContext, TParent;

        IObjectWatcher<TParent> CreateWatcher(Func<IEnumerable<int>, Binding[]> bindingsFactory);
    }

    internal interface IBindingNodeRoot<TContext> : IBindingNode<TContext, TContext>
    {
        IBindingNodeRoot<TNewContext> CloneRootForDerivedType<TNewContext>()
            where TNewContext : class, TContext;
    }

    internal interface IObjectWatcher<in TParent> : IDisposable
    {
        void Attach(TParent parent);
    }
}
