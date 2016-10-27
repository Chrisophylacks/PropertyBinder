namespace PropertyBinder.Engine
{
    internal abstract class Binding
    {
        public bool IsScheduled;

        public abstract string Key { get; }

        public abstract object Context { get; }

        public abstract void Execute();
    }
}