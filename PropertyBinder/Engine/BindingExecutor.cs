using System;
using System.Collections.Generic;

namespace PropertyBinder.Engine
{
    internal class BindingExecutor
    {
        [ThreadStatic]
        private static BindingExecutor _instance;

        public static void Execute(Binding[] bindings)
        {
            var instance = _instance;
            if (instance == null)
            {
                _instance = instance = new BindingExecutor();
            }

            instance.ExecuteInternal(bindings);
        }

        private readonly Queue<Binding> _bindings = new Queue<Binding>();
        private bool _isExecuting;

        private void ExecuteInternal(Binding[] bindings)
        {
            foreach (var binding in bindings)
            {
                if (!binding.IsScheduled)
                {
                    _bindings.Enqueue(binding);
                    binding.IsScheduled = true;
                }
            }

            if (!_isExecuting)
            {
                try
                {
                    _isExecuting = true;
                    while (_bindings.Count > 0)
                    {
                        var binding = _bindings.Dequeue();
                        binding.IsScheduled = false;
                        binding.Action();
                    }
                }
                catch (Exception)
                {
                    foreach (var binding in _bindings)
                    {
                        binding.IsScheduled = false;
                    }
                    _bindings.Clear();
                    throw;
                }
                finally
                {
                    _isExecuting = false;
                }
            }
        }
    }
}