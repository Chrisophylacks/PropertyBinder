using System;
using System.Diagnostics;
using PropertyBinder.Engine;

namespace PropertyBinder.Diagnostics
{
    internal sealed class DebugContext
    {
        private readonly StackFrame _frame;
        private Action<BindingReference[], int> _virtualFrame;

        public DebugContext(string description, StackFrame frame)
        {
            _frame = frame;
            Description = description;
        }

        public string Description { get; }

        public Action<BindingReference[], int> VirtualFrame => _virtualFrame ?? (_virtualFrame = VirtualFrameCompiler.CreateMethodFrame(Description, _frame));
    }
}
