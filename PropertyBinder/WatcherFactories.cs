using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PropertyBinder.Diagnostics;
using PropertyBinder.Engine;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    internal interface IWatcherFactory<in TContext>
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
            var map = new BindingMap<TContext>(_actions);
            map.SetContext(context);
            var watcher = _rootNode.CreateWatcher(map);
            watcher.Attach(context);
            return watcher;
        }
    }
    
    internal sealed class ReusableWatcherFactory<TContext> : IWatcherFactory<TContext>
        where TContext : class
    {
        private readonly Binder<TContext>.BindingAction[] _actions;
        private readonly IBindingNode<TContext> _root;

        private readonly ConcurrentStack<WeakReference> _detachedWatchers = new ConcurrentStack<WeakReference>();

        public ReusableWatcherFactory(Binder<TContext>.BindingAction[] actions, IBindingNode<TContext> root)
        {
            _actions = actions;
            _root = root;
        }

        public IDisposable Attach(TContext context)
        {
            Root root = null;
            while (_detachedWatchers.TryPop(out var reference))
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

        private sealed class Root : IDisposable
        {
            private readonly ReusableWatcherFactory<TContext> _parent;
            private readonly IObjectWatcher<TContext> _watcher;
            private readonly BindingMap<TContext> _map;

            public Root(ReusableWatcherFactory<TContext> parent)
            {
                _parent = parent;
                _map = new BindingMap<TContext>(parent._actions);
                _watcher = _parent._root.CreateWatcher(_map);
            }

            public void SetContext(TContext context)
            {
                _map.SetContext(context);
                _watcher.Attach(context);
            }

            public void Dispose()
            {
                _watcher.Attach(null);
                _parent._detachedWatchers.Push(new WeakReference(this));
            }
        }
    }
}