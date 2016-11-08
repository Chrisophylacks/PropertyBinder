using System.Diagnostics;
using System.Linq.Expressions;

namespace PropertyBinder.Diagnostics
{
    internal sealed class DebugContextBuilder
    {
        private static readonly bool IsDebuggerAttached;

        private readonly StackFrame _frame;
        private readonly string _sourceDescription;

        static DebugContextBuilder()
        {
            IsDebuggerAttached = Debugger.IsAttached;
        }

        public DebugContextBuilder(int frameNumber, Expression source, string comment)
        {
            _sourceDescription = source + comment;
            if (IsDebuggerAttached)
            {
                _frame = new StackTrace(true).GetFrame(frameNumber + 1);
            }
        }

        public DebugContext CreateContext(string targetClassName, string targetKey)
        {
            return new DebugContext(string.Format("{0} To {1}.{2}", _sourceDescription, targetClassName, targetKey ?? "SomeAction"), _frame);
        }
    }
}