using System;

namespace PropertyBinder
{
    [Obsolete("Preserved for compatibility. Please use Binder class instead.")]
    public sealed class PropertyBinder<TContext>
        where TContext : class
    {
        internal Binder<TContext> Binder { get; private set; }

        private PropertyBinder(Binder<TContext> binder)
        {
            Binder = new Binder<TContext>();
        }

        public PropertyBinder()
            : this(new Binder<TContext>())
        {
        }

        public PropertyBinder<TNewContext> Clone<TNewContext>()
            where TNewContext : class, TContext
        {
            return new PropertyBinder<TNewContext>(Binder.Clone<TNewContext>());
        }

        public PropertyBinder<TContext> Clone()
        {
            return Clone<TContext>();
        }

        public IDisposable Attach(TContext context)
        {
            return Binder.Attach(context);
        }
    }
}
