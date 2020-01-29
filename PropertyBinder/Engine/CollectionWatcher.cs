using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PropertyBinder.Engine
{
    internal class CollectionWatcher<TCollection, TItem> : IObjectWatcher<TCollection>
        where TCollection : IEnumerable<TItem>
    {
        private readonly CollectionBindingNode<TCollection, TItem> _node;
        private readonly BindingMap _map;
        private readonly IDictionary<TItem, IObjectWatcher<TItem>> _attachedItems = new Dictionary<TItem, IObjectWatcher<TItem>>();

        protected TCollection _target;

        public CollectionWatcher(CollectionBindingNode<TCollection, TItem> node, BindingMap map)
        {
            _node = node;
            _map = map;
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

            if (_target != null && _node.ItemNode != null)
            {
                AttachItems();
            }
        }

        public void Dispose()
        {
            Attach(default(TCollection));
        }

        protected virtual void TargetCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_node.Indexes.Length > 0)
            {
                BindingExecutor.Execute(_map, _node.Indexes);
            }

            if (_node.ItemNode != null)
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
                var watcher = _node.ItemNode.CreateWatcher(_map);
                watcher.Attach(item);
                _attachedItems.Add(item, watcher);
            }
        }
    }
}