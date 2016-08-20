using System;
using System.Collections.Generic;
using System.Linq;

namespace PropertyBinder.Engine
{
    internal class BindingNode<TContext, TParent, TNode> : IBindingNode<TContext, TParent>
    {
        protected readonly Func<TParent, TNode> _targetSelector;
        protected readonly IDictionary<string, List<int>> _bindingActions;
        protected IDictionary<string, IBindingNode<TContext, TNode>> _subNodes;
        protected ICollectionBindingNode<TContext, TNode> _collectionNode;

        protected BindingNode(Func<TParent, TNode> targetSelector, IDictionary<string, IBindingNode<TContext, TNode>> subNodes, IDictionary<string, List<int>> bindingActions, ICollectionBindingNode<TContext, TNode> collectionNode)
        {
            _targetSelector = targetSelector;
            _subNodes = subNodes;
            _bindingActions = bindingActions;
            _collectionNode = collectionNode;
        }

        public BindingNode(Func<TParent, TNode> targetSelector)
            : this(targetSelector, null, new Dictionary<string, List<int>>(), null)
        {
            _targetSelector = targetSelector;
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

        public IBindingNode<TContext> GetSubNode(BindableMember member)
        {
            if (_subNodes == null)
            {
                _subNodes = new Dictionary<string, IBindingNode<TContext, TNode>>();
            }

            IBindingNode<TContext, TNode> node;
            if (!_subNodes.TryGetValue(member.Name, out node))
            {
                var selector = member.CreateSelector(typeof(TNode));
                node = (IBindingNode<TContext, TNode>)Activator.CreateInstance(typeof(BindingNode<,,>).MakeGenericType(typeof(TContext), typeof(TNode), selector.Method.ReturnType), selector);
                _subNodes.Add(member.Name, node);
            }

            return node;
        }

        public ICollectionBindingNode<TContext> GetCollectionNode(Type itemType)
        {
            return _collectionNode ?? (_collectionNode = (ICollectionBindingNode<TContext, TNode>) Activator.CreateInstance(typeof (CollectionBindingNode<,,>).MakeGenericType(typeof (TContext), typeof (TNode), itemType)));
        }

        public void AddAction(string memberName, int actionIndex)
        {
            List<int> currentAction;
            if (!_bindingActions.TryGetValue(memberName, out currentAction))
            {
                _bindingActions[memberName] = currentAction = new List<int>();
            }
            currentAction.Add(actionIndex);
        }

        public IObjectWatcher<TParent> CreateWatcher(Func<IEnumerable<int>, Binding[]> bindingsFactory)
        {
            return new ObjectWatcher<TParent, TNode>(
                _targetSelector,
                CreateSubWatchers(bindingsFactory),
                _bindingActions.ToDictionary(x => x.Key, x => bindingsFactory(x.Value)));
        }

        public IBindingNode<TNewContext, TParent> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext
        {
            return new BindingNode<TNewContext, TParent, TNode>(
                _targetSelector,
                _subNodes != null ? _subNodes.ToDictionary(x => x.Key, x => x.Value.CloneForDerivedType<TNewContext>()) : null,
                _bindingActions.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
                _collectionNode != null ? _collectionNode.CloneForDerivedType<TNewContext>() : null);
        }

        public IBindingNode<TNewContext, TNewContext> CloneSubRootForDerivedType<TNewContext>()
            where TNewContext : class, TContext, TParent
        {
            return new BindingNode<TNewContext, TNewContext, TNode>(
                _targetSelector,
                _subNodes != null ? _subNodes.ToDictionary(x => x.Key, x => x.Value.CloneForDerivedType<TNewContext>()) : null,
                _bindingActions.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
                _collectionNode != null ? _collectionNode.CloneForDerivedType<TNewContext>() : null);
        }

        private IDictionary<string, IObjectWatcher<TNode>> CreateSubWatchers(Func<IEnumerable<int>, Binding[]> bindingsFactory)
        {
            var dict = _subNodes != null ? _subNodes.ToDictionary(x => x.Key, x => x.Value.CreateWatcher(bindingsFactory)) : null;
            if (_collectionNode != null)
            {
                if (dict == null)
                {
                    dict = new Dictionary<string, IObjectWatcher<TNode>>();
                }

                dict.Add("$<binding>collection", _collectionNode.CreateWatcher(bindingsFactory));
            }

            return dict;
        }
    }

    internal sealed class BindingNodeRoot<TContext> : BindingNode<TContext, TContext, TContext>, IBindingNodeRoot<TContext>
    {
        private BindingNodeRoot(Func<TContext, TContext> targetSelector, IDictionary<string, IBindingNode<TContext, TContext>> subNodes, IDictionary<string, List<int>> bindingActions, ICollectionBindingNode<TContext, TContext> collectionNode)
            : base(targetSelector, subNodes, bindingActions, collectionNode)
        {
        }

        public BindingNodeRoot()
            : base(_ => _)
        {
        }

        public IBindingNodeRoot<TNewContext> CloneRootForDerivedType<TNewContext>()
            where TNewContext : class, TContext
        {
            return new BindingNodeRoot<TNewContext>(
               _ => _,
               _subNodes != null ? _subNodes.ToDictionary(x => x.Key, x => x.Value.CloneSubRootForDerivedType<TNewContext>()) : null,
               _bindingActions.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
               _collectionNode != null ? _collectionNode.CloneForDerivedType<TNewContext>() : null);
        }
    }
}