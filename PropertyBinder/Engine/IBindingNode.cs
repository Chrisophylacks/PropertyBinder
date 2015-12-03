using System;
using System.Reflection;

namespace PropertyBinder.Engine
{
    internal interface IBindingNode<out TContext>
    {
        IBindingNode<TContext> GetSubNode(MemberInfo member);

        ICollectionBindingNode<TContext> GetCollectionNode(Type itemType);

        void AddAction(PropertyInfo property, Action<TContext> action);

        void RemoveActionCascade(Action<TContext> action);
    }

    internal interface IBindingNode<TContext, in TParent> : IBindingNode<TContext>
    {
        IBindingNode<TNewContext, TParent> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext;

        IObjectWatcher<TParent> CreateWatcher(TContext context);
    }

    internal interface IObjectWatcher<in TParent> : IDisposable
    {
        void Attach(TParent parent);
    }
}
