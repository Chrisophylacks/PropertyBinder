using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Input;
using PropertyBinder.Diagnostics;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    public sealed class CommandRuleBinder<TContext>
        where TContext : class
    {
        private readonly Binder<TContext> _binder;
        private readonly Action<TContext, object> _executeAction;
        private readonly Expression<Func<TContext, object, bool>> _canExecuteExpression;
        private readonly bool _hasParameter;
        private readonly List<Expression> _dependencies = new List<Expression>();
        private string _key;
        private readonly DebugContextBuilder _debugContext;
        private CommandCanExecuteCheckMode _canExecuteCheckMode;

        internal CommandRuleBinder(Binder<TContext> binder, Action<TContext, object> executeAction, Expression<Func<TContext, object, bool>> canExecuteExpression, bool hasParameter)
        {
            _debugContext = new DebugContextBuilder(canExecuteExpression.Body, null);
            _binder = binder;
            _executeAction = executeAction;
            _canExecuteExpression = canExecuteExpression;
            _hasParameter = hasParameter;
            _canExecuteCheckMode = Binder.DefaultCommandCanExecuteCheckMode;
        }

        public CommandRuleBinder<TContext> OverrideKey(string bindingRuleKey)
        {
            _key = bindingRuleKey;
            return this;
        }

        public CommandRuleBinder<TContext> WithDependency<TDependency>(Expression<Func<TContext, TDependency>> dependencyExpression)
        {
            _dependencies.Add(dependencyExpression.Body);
            return this;
        }

        public CommandRuleBinder<TContext> WithCanExecuteCheckMode(CommandCanExecuteCheckMode mode)
        {
            _canExecuteCheckMode = mode;
            return this;
        }

        public void To(Expression<Func<TContext, ICommand>> destinationExpression)
        {
            var contextParameter = destinationExpression.Parameters[0];
            var commandParameter = Expression.Parameter(typeof(ICommand));

            var assignCommand = 
                Binder.ExpressionCompiler.Compile(
                    Expression.Lambda<Action<TContext, ICommand>>(
                        Expression.Assign(
                            destinationExpression.Body,
                            commandParameter),
                        contextParameter,
                        commandParameter));

            var getCommand = Binder.ExpressionCompiler.Compile(destinationExpression);
            var canExecute = Binder.ExpressionCompiler.Compile(_canExecuteExpression);
            var key = _key ?? destinationExpression.GetTargetKey();
            _dependencies.Add(_canExecuteExpression);

            _binder.AddRule(ctx => assignCommand(ctx, new ActionCommand(ctx, _executeAction, canExecute, _hasParameter, _canExecuteCheckMode)), key, _debugContext.CreateContext(typeof(TContext).Name, key), true, true, null, Enumerable.Empty<LambdaExpression>());
            _binder.AddRule(ctx => UpdateCanExecuteOnCommand(getCommand(ctx)), key, _debugContext.CreateContext(typeof(TContext).Name, key + "_CanExecute"), true, false, null, _dependencies);
        }

        private sealed class ActionCommand : ICommand
        {
            private readonly TContext _context;
            private readonly Action<TContext, object> _action;
            private readonly Func<TContext, object, bool> _getCanExecute;
            private readonly bool _hasParameter;
            private readonly CommandCanExecuteCheckMode _canExecuteCheckMode;
            private bool _canExecute;

            public ActionCommand(TContext context, Action<TContext, object> action, Func<TContext, object, bool> getCanExecute, bool hasParameter, CommandCanExecuteCheckMode canExecuteCheckMode)
            {
                _context = context;
                _action = action;
                _getCanExecute = getCanExecute;
                _hasParameter = hasParameter;
                _canExecuteCheckMode = canExecuteCheckMode;
            }

            public bool CanExecute(object parameter)
            {
                return _hasParameter ? _getCanExecute(_context, parameter) : _canExecute;
            }

            public void Execute(object parameter)
            {
                if (_canExecuteCheckMode != CommandCanExecuteCheckMode.DoNotCheck && !CanExecute(parameter))
                {
                    switch (_canExecuteCheckMode)
                    {
                        case CommandCanExecuteCheckMode.DoNotExecute:
                            return;

                        case CommandCanExecuteCheckMode.ThrowException:
                            throw new InvalidOperationException("Command cannot be executed");
                    }
                }
                _action(_context, parameter);
            }

            public void UpdateCanExecute()
            {
                if (_hasParameter)
                {
                    OnCanExecuteChanged();
                }
                else
                {
                    var value = _getCanExecute(_context, null);
                    if (_canExecute != value)
                    {
                        _canExecute = value;
                        OnCanExecuteChanged();
                    }
                }
            }

            public event EventHandler CanExecuteChanged;

            private void OnCanExecuteChanged()
            {
                var call = CanExecuteChanged;
                if (call != null)
                {
                    call(this, EventArgs.Empty);
                }
            }
        }

        private static void UpdateCanExecuteOnCommand(ICommand command)
        {
            var actionCommand = command as ActionCommand;
            if (actionCommand != null)
            {
                actionCommand.UpdateCanExecute();
            }
        }
    }

    public enum CommandCanExecuteCheckMode
    {
        DoNotCheck,
        DoNotExecute,
        ThrowException
    }
}