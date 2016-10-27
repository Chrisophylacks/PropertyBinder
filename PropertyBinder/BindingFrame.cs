namespace PropertyBinder
{
    public sealed class BindingFrame
    {
        public BindingFrame(string key, object context)
        {
            Key = key;
            Context = context;
        }

        public string Key { get; private set; }

        public object Context { get; private set; }
    }
}