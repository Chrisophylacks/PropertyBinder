using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine
{
    internal abstract class BindingMap
    {
        // seems to work faster than BitArray, we don't need to optimize for memory THAT much
        public readonly bool[] Schedule;

        protected BindingMap(int size)
        {
            Schedule = new bool[size];
        }

        public abstract void Execute(int index);

        public abstract string GetStamp(int index);

        public abstract DebugContext GetDebugContext(int index);
    }

    internal sealed class BindingMap<TContext> : BindingMap
        where TContext : class
    {
        public readonly Binder<TContext>.BindingAction[] _actions;

        private TContext _context;

        public BindingMap(Binder<TContext>.BindingAction[] actions)
            : base(actions.Length)
        {
            _actions = actions;
        }

        public void SetContext(TContext context)
        {
            _context = context;
        }

        public override void Execute(int index)
        {
            _actions[index].Action(_context);
        }

        public override string GetStamp(int index)
        {
            return _actions[index].Stamped?.Invoke(_context) ?? "";
        }

        public override DebugContext GetDebugContext(int index)
        {
            return _actions[index].DebugContext;
        }
    }

    internal sealed class TransactionBindingMap<T> : BindingMap
    {
        private static readonly DebugContext TransactionDebugContext = new DebugContext("Transaction", null);

        public readonly T Parent;

        public TransactionBindingMap(T parent)
            : base(0)
        {
            Parent = parent;
        }

        public override void Execute(int index)
        {
        }

        public override string GetStamp(int index)
        {
            return "";
        }


        public override DebugContext GetDebugContext(int index)
        {
            return TransactionDebugContext;
        }
    }
}