using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using PropertyBinder.Diagnostics;
using PropertyBinder.Engine;
using PropertyBinder.Visitors;

namespace PropertyBinder
{
    public sealed class Binder<TContext>
        where TContext : class
    {
        private readonly IBindingNode<TContext> _rootNode;
        private readonly List<BindingAction> _actions;
        private IWatcherFactory<TContext> _factory;

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
            return new Binder<TNewContext>(
                _rootNode.CloneForDerivedParentType<TNewContext>(),
                _actions.Select(x => x != null ? new Binder<TNewContext>.BindingAction(x.Action, x.Key, x.DebugContext, x.RunOnAttach) : null).ToList());
        }

        public Binder<TContext> Clone()
        {
            return Clone<TContext>();
        }

        public IDisposable BeginTransaction()
        {
            return Binder.BeginTransaction();
        }

        internal void AddRule(Action<TContext> bindingAction, string key, DebugContext debugContext, bool runOnAttach, bool canOverride, IEnumerable<Expression> triggerExpressions)
        {
            if (!string.IsNullOrEmpty(key) && canOverride)
            {
                RemoveRule(key);
            }

            _actions.Add(new BindingAction(bindingAction, key, debugContext, runOnAttach));

            foreach (var expr in triggerExpressions)
            {
                new BindingExpressionVisitor<TContext>(_rootNode, typeof(TContext), _actions.Count - 1).Visit(expr);
            }
        }

        internal void RemoveRule(string key)
        {
            for (int i = 0; i < _actions.Count; ++i)
            {
                if (_actions[i] != null && _actions[i].Key == key)
                {
                    _actions[i] = null;
                }
            }
        }

        public Action<TContext> GetActionByKey(string key)
        {
            Action<TContext> result = null;

            for (int i = 0; i < _actions.Count; ++i)
            {
                if (_actions[i] != null && _actions[i].Key == key)
                {
                    result += _actions[i].Action;
                }
            }

            return result ?? (_ => { });
        }

        public IDisposable Attach(TContext context)
        {
            if (_factory == null)
            {
                _factory = Binder.AllowReuseOfWatchers
                    ? (IWatcherFactory<TContext>)new ReusableWatcherFactory<TContext>(_actions.ToArray(), _rootNode)
                    : new DefaultWatcherFactory<TContext>(_actions.ToArray(), _rootNode);
            }

            var watcher = _factory.Attach(context);

            foreach (var action in _actions)
            {
                if (action != null && action.RunOnAttach)
                {
                    action.Action(context);
                }
            }

            return watcher;
        }

        internal sealed class BindingAction
        {
            public BindingAction(Action<TContext> action, string key, DebugContext debugContext, bool runOnAttach)
            {
                Action = action;
                Key = key;
                DebugContext = debugContext;
                RunOnAttach = runOnAttach;
            }

            public readonly Action<TContext> Action;

            public readonly bool RunOnAttach;

            public readonly string Key;

            public readonly DebugContext DebugContext;
        }

    }

    public static class Binder
    {
        private static bool _debugMode = Debugger.IsAttached;

        private sealed class BindingTransaction : IDisposable
        {
            public BindingTransaction()
            {
                BindingExecutor.Suspend();
            }

            public void Dispose()
            {
                BindingExecutor.Resume();
            }
        }

        public static IDisposable BeginTransaction()
        {
            return new BindingTransaction();
        }

        public static void SetTracer(IBindingTracer tracer)
        {
            BindingExecutor.SetTracer(tracer);
        }

        public static bool DebugMode
        {
            get => _debugMode;
            set
            {
                if (_debugMode != value)
                {
                    _debugMode = value;
                    BindingExecutor.ResetInstance();
                }
            }
        }

        public static IExpressionCompiler ExpressionCompiler { get; set; } = DefaultExpressionCompiler.Instance;

        public static CommandCanExecuteCheckMode DefaultCommandCanExecuteCheckMode { get; set; } = CommandCanExecuteCheckMode.DoNotCheck;

        public static bool AllowReuseOfWatchers { get; set; } = true;
    }
}