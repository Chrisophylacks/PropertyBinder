using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PropertyBinder.Diagnostics;
using PropertyBinder.Engine;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    internal interface IWatcherFactory<TContext>
    {
        IDisposable Attach(TContext context);
    }

    internal sealed class DefaultWatcherFactory<TContext> : IWatcherFactory<TContext>
        where TContext : class
    {
        private readonly Binder<TContext>.BindingAction[] _actions;
        private readonly IBindingNode<TContext> _rootNode;

        public DefaultWatcherFactory(Binder<TContext>.BindingAction[] actions, IBindingNode<TContext> rootNode)
        {
            _actions = actions;
            _rootNode = rootNode;
        }

        public IDisposable Attach(TContext context)
        {
            var bindings = new ContextualBinding[_actions.Length];
            for (int i = 0; i < bindings.Length; ++i)
            {
                var action = _actions[i];
                if (action != null)
                {
                    bindings[i] = new ContextualBinding(action, context);
                }
            }

            var factory = new Func<ICollection<int>, Binding[]>(r => bindings.CompactSelect<ContextualBinding, Binding>(r));
            var watcher = _rootNode.CreateWatcher(factory);
            watcher.Attach(context);
            return watcher;
        }

        private sealed class ContextualBinding : Binding
        {
            private readonly Binder<TContext>.BindingAction _bindingAction;
            private readonly TContext _context;

            public ContextualBinding(Binder<TContext>.BindingAction bindingAction, TContext context)
            {
                _bindingAction = bindingAction;
                _context = context;
            }

            public override DebugContext DebugContext => _bindingAction.DebugContext;

            public override void Execute()
            {
                _bindingAction.Action(_context);
            }
        }
    }

    internal sealed class ReusableWatcherFactory<TContext> : IWatcherFactory<TContext>
        where TContext : class
    {
        private readonly Binder<TContext>.BindingAction[] _actions;
        private readonly IBindingNode<TContext> _root;

        private readonly ConcurrentBag<WeakReference> _detachedWatchers = new ConcurrentBag<WeakReference>();

        public ReusableWatcherFactory(Binder<TContext>.BindingAction[] actions, IBindingNode<TContext> root)
        {
            _actions = actions;
            _root = root;
        }

        public IDisposable Attach(TContext context)
        {
            Root root = null;
            while (_detachedWatchers.TryTake(out var reference))
            {
                var target = reference.Target;
                if (reference.IsAlive && (root = target as Root) != null)
                {
                    break;
                }
            }

            if (root == null)
            {
                root = new Root(this);
            }

            root.SetContext(context);
            return root;
        }

        private sealed class ContextualBinding : Binding
        {
            private readonly Binder<TContext>.BindingAction _bindingAction;
            private TContext _context;

            public ContextualBinding(Binder<TContext>.BindingAction bindingAction)
            {
                _bindingAction = bindingAction;
            }

            public override DebugContext DebugContext => _bindingAction.DebugContext;

            public void SetContext(TContext context)
            {
                _context = context;
            }

            public override void Execute()
            {
                _bindingAction.Action(_context);
            }
        }

        private sealed class Root : IDisposable
        {
            private readonly ReusableWatcherFactory<TContext> _parent;
            private readonly ContextualBinding[] _bindings;
            private readonly IObjectWatcher<TContext> _watcher;

            public Root(ReusableWatcherFactory<TContext> parent)
            {
                _parent = parent;
                _bindings = new ContextualBinding[parent._actions.Length];
                for (int i = 0; i < _bindings.Length; ++i)
                {
                    var action = parent._actions[i];
                    if (action != null)
                    {
                        _bindings[i] = new ContextualBinding(action);
                    }
                }

                _watcher = _parent._root.CreateWatcher(r => _bindings.CompactSelect<ContextualBinding, Binding>(r));
            }

            public void SetContext(TContext context)
            {
                foreach (var binding in _bindings)
                {
                    binding?.SetContext(context);
                }
                _watcher.Attach(context);
            }

            public void Dispose()
            {
                _watcher.Attach(null);
                _parent._detachedWatchers.Add(new WeakReference(this));
            }
        }

    }
}