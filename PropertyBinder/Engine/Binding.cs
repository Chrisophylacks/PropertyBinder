using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine
{
    internal abstract class Binding
    {
        public readonly DebugContext DebugContext;

        public bool IsScheduled;

        protected Binding(DebugContext debugContext)
        {
            DebugContext = debugContext;
        }

        public abstract object Context { get; }

        public abstract void Execute();
    }
}