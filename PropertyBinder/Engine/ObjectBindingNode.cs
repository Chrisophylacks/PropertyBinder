using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine
{
    internal sealed class BindingNode<TContext, TParent, TNode> : IBindingNode<TContext, TParent>
    {
        private readonly Func<TParent, TNode> _targetSelector;
        private readonly IDictionary<string, UniqueActionCollection<TContext>> _bindingActions;
        private IDictionary<string, IBindingNode<TContext, TNode>> _subNodes;
        private ICollectionBindingNode<TContext, TNode> _collectionNode;

        private BindingNode(Func<TParent, TNode> targetSelector, IDictionary<string, IBindingNode<TContext, TNode>> subNodes, IDictionary<string, UniqueActionCollection<TContext>> bindingActions, ICollectionBindingNode<TContext, TNode> collectionNode)
        {
            _targetSelector = targetSelector;
            _subNodes = subNodes;
            _bindingActions = bindingActions;
            _collectionNode = collectionNode;
        }

        public BindingNode(Func<TParent, TNode> targetSelector)
            : this(targetSelector, null, new Dictionary<string, UniqueActionCollection<TContext>>(), null)
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

        public IBindingNode<TContext> GetSubNode(MemberInfo member)
        {
            var property = member as PropertyInfo;
            string memberName;
            if (property != null)
            {
                memberName = property.Name;
                if (!typeof (TNode).IsValueType)
                {
                    return GetOrCreateNode(memberName, () => property.GetGetMethod(true).CreateDelegate(typeof (Func<,>).MakeGenericType(typeof (TNode), property.PropertyType)));
                }
            }
            else
            {
                var field = (FieldInfo)member;
                memberName = field.Name;
            }

            return GetOrCreateNode(memberName, () =>
            {
                var parameter = Expression.Parameter(typeof (TNode));
                return Expression.Lambda(Expression.MakeMemberAccess(parameter, member), parameter).Compile();
            });
        }

        private IBindingNode<TContext> GetOrCreateNode(string key, Func<Delegate> createSelector)
        {
            if (_subNodes == null)
            {
                _subNodes = new Dictionary<string, IBindingNode<TContext, TNode>>();
            }

            IBindingNode<TContext, TNode> node;
            if (!_subNodes.TryGetValue(key, out node))
            {
                var selector = createSelector();
                node = (IBindingNode<TContext, TNode>) Activator.CreateInstance(typeof (BindingNode<,,>).MakeGenericType(typeof (TContext), typeof (TNode), selector.Method.ReturnType), selector);
                _subNodes.Add(key, node);
            }

            return node;
        }

        public ICollectionBindingNode<TContext> GetCollectionNode(Type itemType)
        {
            return _collectionNode ?? (_collectionNode = (ICollectionBindingNode<TContext, TNode>) Activator.CreateInstance(typeof (CollectionBindingNode<,,>).MakeGenericType(typeof (TContext), typeof (TNode), itemType)));
        }

        public void AddAction(PropertyInfo property, Action<TContext> action)
        {
            UniqueActionCollection<TContext> currentAction;
            if (!_bindingActions.TryGetValue(property.Name, out currentAction))
            {
                _bindingActions[property.Name] = currentAction = new UniqueActionCollection<TContext>();
            }
            currentAction.Add(action);
        }

        public void RemoveActionCascade(Action<TContext> action)
        {
            foreach (var pair in _bindingActions.ToArray())
            {
                pair.Value.Remove(action);
                if (pair.Value.IsEmpty)
                {
                    _bindingActions.Remove(pair.Key);
                }
            }

            if (_collectionNode != null)
            {
                _collectionNode.RemoveActionCascade(action);
            }

            if (_subNodes != null)
            {
                foreach (var node in _subNodes.Values)
                {
                    node.RemoveActionCascade(action);
                }
            }
        }

        public IObjectWatcher<TParent> CreateWatcher(TContext context)
        {
            return new ObjectWatcher<TContext, TParent, TNode>(
                context,
                _targetSelector,
                CreateSubWatchers(context),
                _bindingActions);
        }

        public IBindingNode<TNewContext, TParent> CloneForDerivedType<TNewContext>()
            where TNewContext : class, TContext
        {
            return new BindingNode<TNewContext, TParent, TNode>(
                _targetSelector,
                _subNodes != null ? _subNodes.ToDictionary(x => x.Key, x => x.Value.CloneForDerivedType<TNewContext>()) : null,
                _bindingActions.ToDictionary(x => x.Key, x => x.Value.Clone<TNewContext>()),
                _collectionNode != null ? _collectionNode.CloneForDerivedType<TNewContext>() : null);
        }

        private IDictionary<string, IObjectWatcher<TNode>> CreateSubWatchers(TContext context)
        {
            var dict = _subNodes != null ? _subNodes.ToDictionary(x => x.Key, x => x.Value.CreateWatcher(context)) : null;
            if (_collectionNode != null)
            {
                if (dict == null)
                {
                    dict = new Dictionary<string, IObjectWatcher<TNode>>();
                }

                dict.Add("$<binding>collection", _collectionNode.CreateWatcher(context));
            }

            return dict;
        }
    }
}