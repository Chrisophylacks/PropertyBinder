using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PropertyBinder.Engine;
using PropertyBinder.Visitors;

namespace PropertyBinder
{
    public sealed class Binder<TContext>
        where TContext : class
    {
        private readonly IBindingNode<TContext> _rootNode;
        private readonly List<BindingAction> _actions;

        private Binder(IBindingNode<TContext> rootNode, List<BindingAction> actions)
        {
            _rootNode = rootNode;
            _actions = actions;
        }

        public Binder()
            : this(new BindingNodeRoot<TContext>(), new List<BindingAction>())
        {
        }

        public Binder<TNewContext> Clone<TNewContext>()
            where TNewContext : class, TContext
        {
            return new Binder<TNewContext>(_rootNode.CloneForDerivedParentType<TNewContext>(), _actions.Select(x => new Binder<TNewContext>.BindingAction(x.Action, x.Key, x.RunOnAttach)).ToList());
        }

        public Binder<TContext> Clone()
        {
            return Clone<TContext>();
        }

        internal void AddRule(Action<TContext> bindingAction, string key, bool runOnAttach, bool canOverride, IEnumerable<Expression> triggerExpressions)
        {
            if (!string.IsNullOrEmpty(key) && canOverride)
            {
                RemoveRule(key);
            }

            _actions.Add(new BindingAction(bindingAction, key, runOnAttach));

            foreach (var expr in triggerExpressions)
            {
                new BindingExpressionVisitor<TContext>(_rootNode, typeof(TContext), _actions.Count - 1).Visit(expr);
            }
        }

        internal void RemoveRule(string key)
        {
            for (int i = 0; i < _actions.Count; ++i)
            {
                if (_actions[i].Key == key)
                {
                    _actions[i] = default(BindingAction);
                }
            }
        }

        public IDisposable Attach(TContext context)
        {
            var dict = new Dictionary<int, Binding>();
            for (int i = 0; i < _actions.Count; ++i)
            {
                var action = _actions[i].Action;
                dict.Add(i, action != null ? new Binding(() => action(context)) : null);
            }

            var factory = new Func<IEnumerable<int>, Binding[]>(e => e.Select(x => dict[x]).Where(x => x != null).ToArray());
            var watcher = _rootNode.CreateWatcher(factory);
            watcher.Attach(context);

            foreach (var action in _actions)
            {
                if (action.Action != null && action.RunOnAttach)
                {
                    action.Action(context);
                }
            }

            return watcher;
        }

        private struct BindingAction
        {
            public BindingAction(Action<TContext> action, string key, bool runOnAttach)
            {
                Action = action;
                Key = key;
                RunOnAttach = runOnAttach;
            }

            public readonly Action<TContext> Action;

            public readonly string Key;

            public readonly bool RunOnAttach;
        }
    }
}