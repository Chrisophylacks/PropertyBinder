using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PropertyBinder.Diagnostics
{
    internal sealed class DebugContextBuilder
    {
        private readonly StackFrame _frame;
        private readonly string _sourceDescription;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DebugContextBuilder(Expression source, string comment)
        {
            _sourceDescription = source + comment;
            if (Binder.DebugMode)
            {
                var stackTrace = new StackTrace(1, true);
                for (int i = 0; i < stackTrace.FrameCount; ++i)
                {
                    var frame = stackTrace.GetFrame(i);
                    if (frame.GetMethod().DeclaringType?.Assembly != Assembly.GetExecutingAssembly())
                    {
                        _frame = frame;
                        break;
                    }
                }
            }
        }

        public DebugContext CreateContext(string targetClassName, string targetKey)
        {
            return new DebugContext(string.Format("{0} -> {1}.{2}", _sourceDescription, targetClassName, targetKey ?? "SomeAction"), _frame);
        }
    }
}