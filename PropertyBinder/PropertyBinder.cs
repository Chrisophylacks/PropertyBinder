using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PropertyBinder.Engine;
using PropertyBinder.Helpers;
using PropertyBinder.Visitors;

namespace PropertyBinder
{
    public sealed class PropertyBinder<TContext>
        where TContext : class
    {
        private readonly IDictionary<string, Action<TContext>> _keyedActions;
        private readonly IBindingNode<TContext, TContext> _rootNode;

        private Action<TContext> _attachActions;

        private PropertyBinder(IBindingNode<TContext, TContext> rootNode, IDictionary<string, Action<TContext>> keyedActions, Action<TContext> attachActions)
        {
            _rootNode = rootNode;
            _keyedActions = keyedActions;
            _attachActions = attachActions;
        }

        public PropertyBinder()
            : this(new BindingNode<TContext, TContext, TContext>(x => x), new Dictionary<string, Action<TContext>>(), null)
        {
        }

        public PropertyBinder<TNewContext> Clone<TNewContext>()
            where TNewContext : class, TContext
        {
            return new PropertyBinder<TNewContext>(_rootNode.CloneForDerivedType<TNewContext>(), _keyedActions.ToDictionary<KeyValuePair<string, Action<TContext>>, string, Action<TNewContext>>(x => x.Key, x => x.Value), _attachActions);
        }

        public PropertyBinder<TContext> Clone()
        {
            return Clone<TContext>();
        }

        internal void AddRule(Action<TContext> bindingAction, string key, bool runOnAttach, bool canOverride, IEnumerable<LambdaExpression> triggerExpressions)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (canOverride)
                {
                    // replace action
                    Action<TContext> existingAction;
                    if (_keyedActions.TryGetValue(key, out existingAction))
                    {
                        _attachActions = _attachActions.RemoveUnique(existingAction);
                        _rootNode.RemoveActionCascade(existingAction);
                    }

                    _keyedActions[key] = bindingAction;
                }
                else
                {
                    // combine action
                    Action<TContext> existingAction;
                    if (_keyedActions.TryGetValue(key, out existingAction))
                    {
                        _keyedActions[key] = existingAction.CombineUnique(bindingAction);
                    }
                    else
                    {
                        _keyedActions.Add(key, bindingAction);
                    }
                }
            }

            if (runOnAttach)
            {
                _attachActions = _attachActions.CombineUnique(bindingAction);
            }

            foreach (var expr in triggerExpressions)
            {
                new BindingExpressionVisitor<TContext>(_rootNode, expr.Parameters[0].Type, bindingAction).Visit(expr);
            }
        }

        internal void RemoveRule(string key)
        {
            Action<TContext> existingAction;
            if (_keyedActions.TryGetValue(key, out existingAction))
            {
                _attachActions = _attachActions.RemoveUnique(existingAction);
                _keyedActions.Remove(key);
                _rootNode.RemoveActionCascade(existingAction);
            }
        }

        public IDisposable Attach(TContext context)
        {
            if (_attachActions != null)
            {
                _attachActions(context);
            }

            var watcher = _rootNode.CreateWatcher(context);
            watcher.Attach(context);
            return watcher;
        }
    }
}
