using System;
using System.Reflection;

namespace PropertyBinder.Engine
{
    internal interface IBindingNode<out TContext>
    {
        bool HasBindingActions { get; }

        IBindingNode<TContext> GetSubNode(MemberInfo member);

        ICollectionBindingNode<TContext> GetCollectionNode(Type itemType);

        void AddAction(PropertyInfo property, Action<TContext> action);

        void RemoveActionCascade(Action<TContext> action);
    }

    internal interface IBindingNode<TContext, in TParent> : IBindingNode<TContext>
    {
        IBindingNode<TNewContext, TParent> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext;

        IBindingNode<TNewContext, TNewContext> CloneSubRootForDerivedType<TNewContext>()
            where TNewContext : class, TContext, TParent;

        IObjectWatcher<TParent> CreateWatcher(TContext context);
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
