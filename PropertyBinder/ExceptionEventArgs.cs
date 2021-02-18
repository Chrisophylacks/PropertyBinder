using System;
using PropertyBinder.Diagnostics;

namespace PropertyBinder
{
    public class ExceptionEventArgs : EventArgs
    {
        internal ExceptionEventArgs(Exception ex, string stampedStr, DebugContext bindingDebugContext = null)
        {
            Exception = ex;
            Description = bindingDebugContext?.Description;
            StampedStr = stampedStr;
        }

        public string Description { get; }

        public Exception Exception { get; }
        public string StampedStr { get; }
        public bool Handled { get; set; }
    }
}