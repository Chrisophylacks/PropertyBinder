using System;
using System.Collections.Generic;

namespace PropertyBinder.Engine
{
    internal interface IBindingNode
    {
        bool HasBindingActions { get; }

        IBindingNode GetSubNode(BindableMember member);

        ICollectionBindingNode GetCollectionNode(Type itemType);

        void AddAction(string propertyName, int actionIndex);
    }

    internal interface IBindingNode<in TParent> : IBindingNode
    {
        IBindingNode<TParent> Clone();

        IBindingNode<TNewParent> CloneForDerivedParentType<TNewParent>()
            where TNewParent : TParent;

        IObjectWatcher<TParent> CreateWatcher(Func<ICollection<int>, Binding[]> bindingsFactory);
    }

    internal interface IObjectWatcher<in TParent> : IDisposable
    {
        void Attach(TParent parent);
    }
}
