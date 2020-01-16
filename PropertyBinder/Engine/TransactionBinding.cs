using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine
{
    internal sealed class TransactionBinding : Binding
    {
        private static readonly DebugContext TransactionDebugContext = new DebugContext("Transaction", null);

        public Binding Parent { get; }

        public TransactionBinding(Binding parent)
        {
            Parent = parent;
        }

        public override DebugContext DebugContext => TransactionDebugContext;

        public override void Execute()
        {
        }
    }
}