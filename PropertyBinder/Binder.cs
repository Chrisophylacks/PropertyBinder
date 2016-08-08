using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PropertyBinder.Engine;
using PropertyBinder.Helpers;
using PropertyBinder.Visitors;

namespace PropertyBinder
{
    public sealed class Binder<TContext>
        where TContext : class
    {
        private readonly IDictionary<string, List<Action<TContext>>> _keyedActions;
        private readonly IBindingNodeRoot<TContext> _rootNode;
        private readonly UniqueActionCollection<TContext> _attachActions;

        internal Binder(IBindingNodeRoot<TContext> rootNode, IDictionary<string, List<Action<TContext>>> keyedActions, UniqueActionCollection<TContext> attachActions)
        {
            _rootNode = rootNode;
            _keyedActions = keyedActions;
            _attachActions = attachActions;
        }

        public Binder()
            : this(new BindingNodeRoot<TContext>(), new Dictionary<string, List<Action<TContext>>>(), new UniqueActionCollection<TContext>())
        {
        }

        public Binder<TNewContext> Clone<TNewContext>()
            where TNewContext : class, TContext
        {
            return new Binder<TNewContext>(_rootNode.CloneRootForDerivedType<TNewContext>(), _keyedActions.ToDictionary(x => x.Key, x => new List<Action<TNewContext>>(x.Value)), _attachActions.Clone<TNewContext>());
        }

        public Binder<TContext> Clone()
        {
            return Clone<TContext>();
        }

        internal void AddRule(Action<TContext> bindingAction, string key, bool runOnAttach, bool canOverride, IEnumerable<Expression> triggerExpressions)
        {
            if (!string.IsNullOrEmpty(key))
            {
                List<Action<TContext>> existingActions;
                if (!_keyedActions.TryGetValue(key, out existingActions))
                {
                    _keyedActions.Add(key, existingActions = new List<Action<TContext>>());
                }

                if (canOverride)
                {
                    foreach (var action in existingActions)
                    {
                        _attachActions.Remove(action);
                        _rootNode.RemoveActionCascade(action);
                    }
                }

                existingActions.Add(bindingAction);
            }

            if (runOnAttach)
            {
                _attachActions.Add(bindingAction);
            }

            foreach (var expr in triggerExpressions)
            {
                new BindingExpressionVisitor<TContext>(_rootNode, typeof(TContext), bindingAction).Visit(expr);
            }
        }

        internal void RemoveRule(string key)
        {
            List<Action<TContext>> existingActions;
            if (_keyedActions.TryGetValue(key, out existingActions))
            {
                foreach (var action in existingActions)
                {
                    _attachActions.Remove(action);
                    _rootNode.RemoveActionCascade(action);
                }

                _keyedActions.Remove(key);
            }
        }

        public IDisposable Attach(TContext context)
        {
            var watcher = _rootNode.CreateWatcher(context);
            watcher.Attach(context);

            if (_attachActions != null)
            {
                _attachActions.Execute(context);
            }

            return watcher;
        }
    }
}