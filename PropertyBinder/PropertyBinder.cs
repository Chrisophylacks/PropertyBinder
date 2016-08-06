using System;

namespace PropertyBinder
{
    [Obsolete("Added for compatibility reasons. Please use Binder class instead")]
    public class PropertyBinder<TContext> : Binder<TContext>
        where TContext : class
    {
        private PropertyBinder(BindingRules<TContext> rules)
            : base(rules)
        {
        }

        public PropertyBinder()
            : this(new BindingRules<TContext>())
        {
        }

        public new PropertyBinder<TNewContext> Clone<TNewContext>()
            where TNewContext : class, TContext
        {
            return new PropertyBinder<TNewContext>(Rules.Clone<TNewContext>());
        }

        public new PropertyBinder<TContext> Clone()
        {
            return Clone<TContext>();
        }
    }
}