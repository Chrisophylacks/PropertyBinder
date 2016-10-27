using System;
using System.Collections.Generic;

namespace PropertyBinder.Engine
{
    internal class BindingExecutor
    {
        [ThreadStatic]
        private static BindingExecutor _instance;

        private static BindingExecutor Instance
        {
            get
            {
                var instance = _instance;
                if (instance == null)
                {
                    _instance = instance = new BindingExecutor();
                }
                return instance;
            }
        }

        public static void Execute(Binding[] bindings)
        {
            Instance.ExecuteInternal(bindings);
        }

        public static void Suspend()
        {
            Instance._executingBinding = new ScheduledBinding(null, Instance._executingBinding);
        }

        public static void Resume()
        {
            Instance._executingBinding = Instance._executingBinding?.Parent;
            Instance.ExecuteInternal(new Binding[0]);
        }

        public static IEnumerable<Binding> TraceBindings()
        {
            var bindings = new List<Binding>();
            var current = Instance._executingBinding;

            while (current != null)
            {
                bindings.Add(current.Binding);
                current = current.Parent;
            }

            bindings.Reverse();

            return bindings;
        }

        private readonly Queue<ScheduledBinding> _scheduledBindings = new Queue<ScheduledBinding>();
        private ScheduledBinding _executingBinding;

        private void ExecuteInternal(Binding[] bindings)
        {
            foreach (var binding in bindings)
            {
                if (!binding.IsScheduled)
                {
                    _scheduledBindings.Enqueue(new ScheduledBinding(binding, _executingBinding));
                    binding.IsScheduled = true;
                }
            }

            if (_executingBinding == null)
            {
                try
                {
                    while (_scheduledBindings.Count > 0)
                    {
                        _executingBinding = _scheduledBindings.Dequeue();
                        _executingBinding.Binding.IsScheduled = false;
                        _executingBinding.Binding.Execute();
                    }
                }
                catch (Exception)
                {
                    foreach (var tp in _scheduledBindings)
                    {
                        tp.Binding.IsScheduled = false;
                    }
                    _scheduledBindings.Clear();
                    throw;
                }
                finally
                {
                    _executingBinding = null;
                }
            }
        }

        private sealed class ScheduledBinding
        {
            public ScheduledBinding(Binding binding, ScheduledBinding parent)
            {
                Binding = binding;
                Parent = parent;
            }

            public readonly Binding Binding;

            public readonly ScheduledBinding Parent;
        }
    }
}