namespace PropertyBinder
{
    public abstract class BindingRuleBase
    {
        protected string _key;
        protected bool _runOnAttach = true;
        protected bool _canOverride = true;

        protected BindingRuleBase()
        {
        }

        internal void SetRuleKey(string key)
        {
            _key = key;
        }

        internal void DoNotRunOnAttach()
        {
            _runOnAttach = false;
        }

        internal void DoNotOverride()
        {
            _canOverride = false;
        }
    }
}