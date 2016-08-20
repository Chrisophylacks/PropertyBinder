using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PropertyBinder.Engine
{
    internal class CollectionWatcher<TContext, TCollection, TItem> : IObjectWatcher<TCollection>
        where TCollection : class, IEnumerable<TItem>
    {
        private readonly Binding[] _ownBindings;
        private readonly Func<IEnumerable<int>, Binding[]> _bindingsFactory;
        private readonly IBindingNode<TContext, TItem> _itemNode;
        private readonly IDictionary<TItem, IObjectWatcher<TItem>> _attachedItems = new Dictionary<TItem, IObjectWatcher<TItem>>();

        protected TCollection _target;

        public CollectionWatcher(Binding[] ownBindings, Func<IEnumerable<int>, Binding[]> bindingsFactory, IBindingNode<TContext, TItem> itemNode)
        {
            _ownBindings = ownBindings;
            _bindingsFactory = bindingsFactory;
            _itemNode = itemNode;
        }

        public void Attach(TCollection parent)
        {
            DetachItems();

            var notify = _target as INotifyCollectionChanged;
            if (notify != null)
            {
                notify.CollectionChanged -= TargetCollectionChanged;
            }

            _target = parent;

            notify = _target as INotifyCollectionChanged;
            if (notify != null)
            {
                notify.CollectionChanged += TargetCollectionChanged;
            }

            if (_target != null && _itemNode != null)
            {
                AttachItems();
            }
        }

        public void Dispose()
        {
            Attach(null);
        }

        protected virtual void TargetCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_ownBindings != null)
            {
                BindingExecutor.Execute(_ownBindings);
            }

            if (_itemNode != null)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    {
                        AttachItem((TItem) e.NewItems[0]);
                        break;
                    }

                    case NotifyCollectionChangedAction.Remove:
                    {
                        DetachItem((TItem)e.OldItems[0]); 
                        break;
                    }

                    case NotifyCollectionChangedAction.Replace:
                    {
                        DetachItem((TItem)e.OldItems[0]);
                        AttachItem((TItem)e.NewItems[0]);
                        break;
                    }

                    case NotifyCollectionChangedAction.Reset:
                    {
                        DetachItems();
                        AttachItems();
                        break;
                    }
                }
            }
        }

        private void DetachItems()
        {
            foreach (var watcher in _attachedItems.Values)
            {
                watcher.Dispose();
            }
            _attachedItems.Clear();
        }

        private void DetachItem(TItem item)
        {
            IObjectWatcher<TItem> watcher;
            if (item != null && _target != null && !_target.Contains(item) && _attachedItems.TryGetValue(item, out watcher))
            {
                watcher.Dispose();
                _attachedItems.Remove(item);
            }
        }

        private void AttachItems()
        {
            foreach (var item in _target)
            {
                AttachItem(item);
            }
        }

        private void AttachItem(TItem item)
        {
            if (item != null && !_attachedItems.ContainsKey(item))
            {
                var watcher = _itemNode.CreateWatcher(_bindingsFactory);
                watcher.Attach(item);
                _attachedItems.Add(item, watcher);
            }
        }
    }
}