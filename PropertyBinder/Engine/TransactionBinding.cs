using PropertyBinder.Diagnostics;

namespace PropertyBinder.Engine
{
    internal sealed class TransactionBinding : Binding
    {
        static TransactionBinding()
        {
            Instance = new TransactionBinding();
        }

        public static TransactionBinding Instance { get; }

        private TransactionBinding()
            : base(new DebugContext("Transaction", null))
        { }

        public override object Context => this;

        public override void Execute()
        {
        }
    }
}