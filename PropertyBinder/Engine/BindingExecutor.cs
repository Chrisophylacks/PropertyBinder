using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PropertyBinder.Engine
{
    internal class BindingExecutor
    {
        [ThreadStatic]
        private static BindingExecutor _instance;

        private static Action<string> _tracer;

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

        public static void SetTracingMethod(Action<string> tracer)
        {
            _tracer = tracer;
        }

        public static void Execute(Binding[] bindings)
        {
            Instance.ExecuteInternal(bindings);
        }

        public static void Suspend()
        {
            Instance._executingBinding = new ScheduledBinding(TransactionBinding.Instance, Instance._executingBinding);
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

        private BindingExecutor()
        {
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
                    _tracer?.Invoke(string.Format("Scheduled binding {0}", binding.DebugContext.Description));
                }
                else
                {
                    _tracer?.Invoke(string.Format("Ignored binding {0}", binding.DebugContext.Description));
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
                        _tracer?.Invoke(string.Format("Executing binding {0}", _executingBinding.Binding.DebugContext.Description));
                        if (Binder.DebugMode)
                        {
                            ExecuteWithVirtualStack();
                        }
                        else
                        {
                            _executingBinding.Binding.Execute();
                        }
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

        private static void ExecuteWithVirtualStack()
        {
            var bindings = TraceBindings().ToArray();
            bindings[0].DebugContext.VirtualFrame(bindings, 0);
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