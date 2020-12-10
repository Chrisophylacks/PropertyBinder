using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PropertyBinder.Diagnostics;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine
{
    internal abstract class BindingExecutor
    {
        [ThreadStatic]
        private static BindingExecutor _instance;
        protected static IBindingTracer _tracer;
        protected static EventHandler<ExceptionEventArgs> _exceptionHandler;

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

        public static void SetExceptionHandler(EventHandler<ExceptionEventArgs> exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
            ResetInstance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Execute(BindingMap map, int[] bindings)
        {
            Instance.ExecuteInternal(map, bindings);
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

        protected abstract void ExecuteInternal(BindingMap map, int[] bindings);

        protected abstract void SuspendInternal();

        protected abstract void ResumeInternal();
    }

    internal sealed class ProductionModeBindingExecutor : BindingExecutor
    {
        private readonly LiteQueue<BindingReference> _scheduledBindings = new LiteQueue<BindingReference>();
        private int _executeLock;

        protected override void SuspendInternal()
        {
            ++_executeLock;
        }

        protected override void ResumeInternal()
        {
            if (_executeLock == 0)
            {
                throw new InvalidOperationException("Binder in not currently in transaction mode");
            }

            --_executeLock;
            ExecuteInternal(null, new int[0]);
        }

        protected override void ExecuteInternal(BindingMap map, int[] bindings)
        {
            _scheduledBindings.Reserve(bindings.Length);
            foreach (var i in bindings)
            {
                if (!map.Schedule[i])
                {
                    map.Schedule[i] = true;
                    _scheduledBindings.EnqueueUnsafe(new BindingReference(map, i));
                }
            }

            if (_executeLock == 0)
            {
                ++_executeLock;
                try
                {
                    while (_scheduledBindings.Count > 0)
                    {
                        ref BindingReference binding = ref _scheduledBindings.DequeueRef();
                        binding.UnSchedule();
                        try
                        {
                            binding.Execute();
                        }
                        catch (Exception e)
                        {
                            var exceptionEventArgs = new ExceptionEventArgs(e, binding.DebugContext);
                            _exceptionHandler?.Invoke(null, exceptionEventArgs);
                            if (!exceptionEventArgs.Handled)
                            {
                                throw;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    while (_scheduledBindings.Count > 0)
                    {
                        _scheduledBindings.DequeueRef().UnSchedule();
                    }
                    throw;
                }
                finally
                {
                    --_executeLock;
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
            _executingBinding = new ScheduledBinding(new BindingReference(new TransactionBindingMap<ScheduledBinding>(_executingBinding), 0), _executingBinding);
        }

        protected override void ResumeInternal()
        {
            _executingBinding = _executingBinding?.Parent;
            ExecuteInternal(null, new int[0]);
        }

        private sealed class ScheduledBinding
        {
            public ScheduledBinding(BindingReference binding, ScheduledBinding parent)
            {
                Binding = binding;
                Parent = parent;
            }

            public readonly BindingReference Binding;

            public readonly ScheduledBinding Parent;
        }

        protected override void ExecuteInternal(BindingMap map, int[] bindings)
        {
            foreach (var i in bindings)
            {
                var binding = new BindingReference(map, i);
                if (binding.Schedule())
                {
                    _scheduledBindings.Enqueue(new ScheduledBinding(binding, _executingBinding));
                    _tracer?.OnScheduled(map.GetDebugContext(i).Description);
                }
                else
                {
                    _tracer?.OnIgnored(map.GetDebugContext(i).Description);
                }
            }

            if (_executingBinding == null)
            {
                try
                {
                    while (_scheduledBindings.Count > 0)
                    {
                        _executingBinding = _scheduledBindings.Dequeue();
                        _executingBinding.Binding.UnSchedule();
                        var description = _executingBinding.Binding.DebugContext?.Description;
                        _tracer?.OnStarted(description);

                        try
                        {
                            if (Binder.DebugMode)
                            {
                                var tracedBindings = TraceBindings().ToArray();
                                var f = tracedBindings[0].DebugContext.VirtualFrame;
                                tracedBindings[0].DebugContext.VirtualFrame(tracedBindings, 0);
                            }
                            else
                            {
                                _executingBinding.Binding.Execute();
                            }
                        }
                        catch (Exception ex)
                        {
                            _tracer?.OnException(ex);
                            var ea = new ExceptionEventArgs(ex, _executingBinding.Binding.DebugContext);
                            _exceptionHandler?.Invoke(this, ea);
                            if (!ea.Handled)
                            {
                                throw;
                            }
                        }

                        _tracer?.OnEnded(description);
                    }
                }
                catch (Exception ex)
                {
                    foreach (var binding in _scheduledBindings)
                    {
                        binding.Binding.UnSchedule();
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

        public IEnumerable<BindingReference> TraceBindings()
        {
            var bindings = new List<BindingReference>();
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

    internal struct BindingReference
    {
        public readonly BindingMap Map;
        public readonly int Index;

        public BindingReference(BindingMap map, int index)
        {
            Map = map;
            Index = index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Schedule()
        {
            if (!Map.Schedule[Index])
            {
                Map.Schedule[Index] = true;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnSchedule()
        {
            Map.Schedule[Index] = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            Map.Execute(Index);
        }

        public DebugContext DebugContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Map.GetDebugContext(Index);
        }
    }
}