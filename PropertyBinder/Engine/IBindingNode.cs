using System;
using System.Collections.Generic;

namespace PropertyBinder.Engine
{
    internal interface IBindingNodeBuilder
    {
        bool HasBindingActions { get; }

        IBindingNodeBuilder GetSubNode(BindableMember member);

        ICollectionBindingNodeBuilder GetCollectionNode(Type itemType);

        void AddAction(string propertyName, int actionIndex);
    }

    internal interface IBindingNodeBuilder<in TParent> : IBindingNodeBuilder
    {
        IBindingNodeBuilder<TParent> Clone();

        IBindingNodeBuilder<TNewParent> CloneForDerivedParentType<TNewParent>()
            where TNewParent : TParent;

        IBindingNode<TParent> CreateBindingNode(int[] actionRemap);
    }

    internal interface IBindingNode<in TParent>
    {
        IObjectWatcher<TParent> CreateWatcher(BindingMap map);
    }

    internal interface IObjectWatcher<in TParent> : IDisposable
    {
        void Attach(TParent parent);
    }
}
