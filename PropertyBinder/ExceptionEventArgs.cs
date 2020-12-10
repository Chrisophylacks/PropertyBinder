using System;
using PropertyBinder.Diagnostics;

namespace PropertyBinder
{
    public class ExceptionEventArgs : EventArgs
    {
        internal ExceptionEventArgs(Exception ex, DebugContext bindingDebugContext = null)
        {
            Exception = ex;
            Description = bindingDebugContext?.Description;
        }

        public string Description { get; }

        public Exception Exception { get; }
        public bool Handled { get; set; }
    }
}