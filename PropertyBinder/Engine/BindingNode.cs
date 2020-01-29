using System;
using System.Collections.Generic;
using System.Linq;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine
{
    internal class BindingNodeBuilder<TParent, TNode> : IBindingNodeBuilder<TParent>
    {
        protected readonly Func<TParent, TNode> _targetSelector;
        protected readonly IDictionary<string, List<int>> _bindingActions;
        protected IDictionary<string, IBindingNodeBuilder<TNode>> _subNodes;
        protected ICollectionBindingNodeBuilder<TNode> _collectionNode;

        protected BindingNodeBuilder(Func<TParent, TNode> targetSelector, IDictionary<string, IBindingNodeBuilder<TNode>> subNodes, IDictionary<string, List<int>> bindingActions, ICollectionBindingNodeBuilder<TNode> collectionNode)
        {
            _targetSelector = targetSelector;
            _subNodes = subNodes;
            _bindingActions = bindingActions;
            _collectionNode = collectionNode;
        }

        public BindingNodeBuilder(Func<TParent, TNode> targetSelector)
            : this(targetSelector, null, new Dictionary<string, List<int>>(), null)
        {
            _targetSelector = targetSelector;
        }

        public IBindingNodeBuilder GetSubNode(BindableMember member)
        {
            if (_subNodes == null)
            {
                _subNodes = new Dictionary<string, IBindingNodeBuilder<TNode>>();
            }

            IBindingNodeBuilder<TNode> node;
            if (!_subNodes.TryGetValue(member.Name, out node))
            {
                var selector = member.CreateSelector(typeof(TNode));
                node = (IBindingNodeBuilder<TNode>)Activator.CreateInstance(typeof(BindingNodeBuilder<,>).MakeGenericType(typeof(TNode), selector.Method.ReturnType), selector);
                _subNodes.Add(member.Name, node);
            }

            return node;
        }

        public ICollectionBindingNodeBuilder GetCollectionNode(Type itemType)
        {
            return _collectionNode ?? (_collectionNode = (ICollectionBindingNodeBuilder<TNode>) Activator.CreateInstance(typeof (CollectionBindingNodeBuilder<,>).MakeGenericType(typeof (TNode), itemType)));
        }

        public void AddAction(string memberName, int actionIndex)
        {
            if (!_bindingActions.TryGetValue(memberName, out var currentAction))
            {
                _bindingActions[memberName] = currentAction = new List<int>();
            }
            currentAction.Add(actionIndex);
        }

        public bool HasBindingActions
        {
            get
            {
                if (_bindingActions.Count != 0)
                {
                    return true;
                }

                if (_subNodes != null && _subNodes.Values.Any(x => x.HasBindingActions))
                {
                    return true;
                }

                if (_collectionNode != null && _collectionNode.HasBindingActions)
                {
                    return true;
                }

                return false;
            }
        }

        public IBindingNodeBuilder<TParent> Clone()
        {
            return new BindingNodeBuilder<TParent, TNode>(
                _targetSelector,
                _subNodes?.ToDictionary(x => x.Key, x => x.Value.Clone()),
                _bindingActions.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
                _collectionNode?.Clone());
        }

        public virtual IBindingNodeBuilder<TNewParent> CloneForDerivedParentType<TNewParent>()
            where TNewParent : TParent
        {
            return new BindingNodeBuilder<TNewParent, TNode>(
                x => _targetSelector(x),
                _subNodes?.ToDictionary(x => x.Key, x => x.Value.Clone()),
                _bindingActions.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
                _collectionNode?.Clone());
        }

        public IBindingNode<TParent> CreateBindingNode(int[] actionRemap)
        {
            return new BindingNode<TParent,TNode>(
                _targetSelector,
                _subNodes?.ToReadOnlyDictionary(x => x.Key, x => x.Value.CreateBindingNode(actionRemap)),
                _bindingActions
                    .Select(pair => new KeyValuePair<string, int[]>(pair.Key, pair.Value.CompactRemap(actionRemap)))
                    .Where(x => x.Value.Length > 0)
                    .ToList()
                    .ToReadOnlyDictionary(x => x.Key, x => x.Value),
                _collectionNode?.CreateBindingNode(actionRemap));
        }
    }

    internal sealed class BindingNodeRootBuilder<TContext> : BindingNodeBuilder<TContext, TContext>
    {
        private BindingNodeRootBuilder(IDictionary<string, IBindingNodeBuilder<TContext>> subNodes, IDictionary<string, List<int>> bindingActions, ICollectionBindingNodeBuilder<TContext> collectionNode)
            : base(_ => _, subNodes, bindingActions, collectionNode)
        {
        }

        public BindingNodeRootBuilder()
            : base(_ => _)
        {
        }

        public override IBindingNodeBuilder<TNewParent> CloneForDerivedParentType<TNewParent>()
        {
            return new BindingNodeRootBuilder<TNewParent>(
                _subNodes?.ToDictionary(x => x.Key, x => x.Value.CloneForDerivedParentType<TNewParent>()),
                _bindingActions.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
                _collectionNode?.CloneForDerivedParentType<TNewParent>());
        }
    }

    internal sealed class BindingNode<TParent, TNode> : IBindingNode<TParent>
    {
        public readonly Func<TParent, TNode> TargetSelector;
        public readonly IReadOnlyDictionary<string, int[]> BindingActions;
        public IReadOnlyDictionary<string, IBindingNode<TNode>> SubNodes;
        public ICollectionBindingNode<TNode> CollectionNode;

        public BindingNode(
            Func<TParent, TNode> targetSelector,
            IReadOnlyDictionary<string, IBindingNode<TNode>> subNodes,
            IReadOnlyDictionary<string, int[]> bindingActions,
            ICollectionBindingNode<TNode> collectionNode)
        {
            TargetSelector = targetSelector;
            SubNodes = subNodes;
            BindingActions = bindingActions;
            CollectionNode = collectionNode;
        }

        public IObjectWatcher<TParent> CreateWatcher(BindingMap map)
        {
            return new ObjectWatcher<TParent, TNode>(this, map);
        }
    }
}