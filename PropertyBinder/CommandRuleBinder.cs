using System;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Input;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    public sealed class CommandRuleBinder<TContext>
        where TContext : class
    {
        private readonly PropertyBinder<TContext> _binder;
        private readonly Action<TContext> _executeAction;
        private readonly Expression<Func<TContext, bool>> _canExecuteExpression;

        public CommandRuleBinder(PropertyBinder<TContext> binder, Action<TContext> executeAction, Expression<Func<TContext, bool>> canExecuteExpression)
        {
            _binder = binder;
            _executeAction = executeAction;
            _canExecuteExpression = canExecuteExpression;
        }

        public void To(Expression<Func<TContext, ICommand>> destinationExpression)
        {
            var contextParameter = destinationExpression.Parameters[0];
            var commandParameter = Expression.Parameter(typeof(ICommand));

            var assignCommand = Expression.Lambda<Action<TContext, ICommand>>(
                Expression.Assign(
                    destinationExpression.Body,
                    commandParameter),
                contextParameter,
                commandParameter).Compile();

            var getCommand = destinationExpression.Compile();
            var canExecute = _canExecuteExpression.Compile();
            var key = destinationExpression.GetTargetKey();

            _binder.AddRule(ctx => assignCommand(ctx, new ActionCommand(ctx, _executeAction, canExecute)), key, true, true, Enumerable.Empty<LambdaExpression>());
            _binder.AddRule(ctx => ((ActionCommand)getCommand(ctx)).UpdateCanExecute(), key, true, false, new[] { _canExecuteExpression });
        }

        private sealed class ActionCommand : ICommand
        {
            private readonly TContext _context;
            private readonly Action<TContext> _action;
            private readonly Func<TContext, bool> _getCanExecute;
            private bool _canExecute;

            public ActionCommand(TContext context, Action<TContext> action, Func<TContext, bool> getCanExecute)
            {
                _context = context;
                _action = action;
                _getCanExecute = getCanExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute;
            }

            public void Execute(object parameter)
            {
                _action(_context);
            }

            public void UpdateCanExecute()
            {
                var value = _getCanExecute(_context);
                if (_canExecute != value)
                {
                    _canExecute = value;
                    var call = CanExecuteChanged;
                    if (call != null)
                    {
                        call(this, EventArgs.Empty);
                    }
                }
            }

            public event EventHandler CanExecuteChanged;
        }
    }
}