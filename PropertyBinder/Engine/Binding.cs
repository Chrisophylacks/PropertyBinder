using System;
using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine
{
    internal abstract class Binding
    {
        public bool IsScheduled;

        public abstract DebugContext DebugContext { get; }

        public abstract void Execute();
    }
}