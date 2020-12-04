using System;

namespace PropertyBinder
{
    public class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception ex)
        {
            this.Exception = ex;
        }
        
        public Exception Exception { get; }
        public bool Handled { get; set; }
    }
}