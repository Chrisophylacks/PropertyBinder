using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine
{
    internal sealed class TransactionBinding : Binding
    {
        public Binding Parent { get; }

        public TransactionBinding(Binding parent)
            : base(new DebugContext("Transaction", null))
        {
            Parent = parent;
        }

        public override object Context => this;

        public override void Execute()
        {
        }
    }
}