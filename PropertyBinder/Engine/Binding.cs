using System;

namespace PropertyBinder.Engine
{
    internal sealed class Binding
    {
        public Binding(Action action)
        {
            Action = action;
        }

        public readonly Action Action;

        public bool IsScheduled;
    }
}