using System;

namespace PropertyBinder
{
    public class Binder<TContext>
        where TContext : class
    {
        internal Binder(BindingRules<TContext> rules)
        {
            Rules = rules;
        }

        public Binder()
            : this(new BindingRules<TContext>())
        {
        }

        internal BindingRules<TContext> Rules { get; private set; }

        public IDisposable Attach(TContext instance)
        {
            return Rules.Attach(instance);
        }

        public Binder<TNewContext> Clone<TNewContext>()
            where TNewContext : class, TContext
        {
            return new Binder<TNewContext>(Rules.Clone<TNewContext>());
        }

        public Binder<TContext> Clone()
        {
            return Clone<TContext>();
        }
    }
}
