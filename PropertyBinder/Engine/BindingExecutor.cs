using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine
{
    internal abstract class BindingExecutor
    {
        [ThreadStatic]
        private static BindingExecutor _instance;

        protected static IBindingTracer _tracer;

        private static BindingExecutor Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _instance ?? ResetInstance();
        }

        public static BindingExecutor ResetInstance()
        {
            _instance = (Binder.DebugMode || _tracer != null) ? (BindingExecutor)new DebugModeBindingExecutor() : new ProductionModeBindingExecutor();
            return _instance;
        }

        public static void SetTracer(IBindingTracer tracer)
        {
            _tracer = tracer;
            ResetInstance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Execute(Binding[] bindings)
        {
            Instance.ExecuteInternal(bindings);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Suspend()
        {
            Instance.SuspendInternal();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Resume()
        {
            Instance.ResumeInternal();
        }

        protected abstract void ExecuteInternal(Binding[] bindings);

        protected abstract void SuspendInternal();

        protected abstract void ResumeInternal();
    }

    internal sealed class ProductionModeBindingExecutor : BindingExecutor
    {
        private readonly Queue<Binding> _scheduledBindings = new Queue<Binding>();
        private Binding _executingBinding;

        protected override void SuspendInternal()
        {
            _executingBinding = new TransactionBinding(_executingBinding);
        }

        protected override void ResumeInternal()
        {
            var transaction = _executingBinding as TransactionBinding;
            if (transaction == null)
            {
                throw new InvalidOperationException("Binder in not currently in transaction mode");
            }

            _executingBinding = transaction.Parent;
            ExecuteInternal(new Binding[0]);
        }

        protected override void ExecuteInternal(Binding[] bindings)
        {
            foreach (var binding in bindings)
            {
                if (!binding.IsScheduled)
                {
                    _scheduledBindings.Enqueue(binding);
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
                        _executingBinding.IsScheduled = false;
                        _executingBinding.Execute();
                    }
                }
                catch (Exception)
                {
                    foreach (var binding in _scheduledBindings)
                    {
                        binding.IsScheduled = false;
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
    }

    internal sealed class DebugModeBindingExecutor : BindingExecutor
    {
        private readonly Queue<ScheduledBinding> _scheduledBindings = new Queue<ScheduledBinding>();
        private ScheduledBinding _executingBinding;

        protected override void SuspendInternal()
        {
            _executingBinding = new ScheduledBinding(new TransactionBinding(_executingBinding.Binding), _executingBinding);
        }

        protected override void ResumeInternal()
        {
            _executingBinding = _executingBinding?.Parent;
            ExecuteInternal(new Binding[0]);
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

        protected override void ExecuteInternal(Binding[] bindings)
        {
            foreach (var binding in bindings)
            {
                if (!binding.IsScheduled)
                {
                    _scheduledBindings.Enqueue(new ScheduledBinding(binding, _executingBinding));
                    binding.IsScheduled = true;
                    _tracer?.OnScheduled(binding.DebugContext.Description);
                }
                else
                {
                    _tracer?.OnIgnored(binding.DebugContext.Description);
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
                        _tracer?.OnStarted(_executingBinding.Binding.DebugContext.Description);

                        if (Binder.DebugMode)
                        {
                            var tracedBindings = TraceBindings().ToArray();
                            tracedBindings[0].DebugContext.VirtualFrame(tracedBindings, 0);
                        }
                        else
                        {
                            _executingBinding.Binding.Execute();
                        }

                        _tracer?.OnEnded(_executingBinding.Binding.DebugContext.Description);
                    }
                }
                catch (Exception ex)
                {
                    _tracer?.OnException(ex);
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

        public IEnumerable<Binding> TraceBindings()
        {
            var bindings = new List<Binding>();
            var current = _executingBinding;

            while (current != null)
            {
                bindings.Add(current.Binding);
                current = current.Parent;
            }

            bindings.Reverse();

            return bindings;
        }
    }
}