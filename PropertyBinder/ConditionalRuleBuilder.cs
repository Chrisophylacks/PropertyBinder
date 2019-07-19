using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PropertyBinder.Diagnostics;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    public interface IConditionalRuleBuilderPhase2<T, TContext>
        where TContext : class
    {
        IConditionalRuleBuilderPhase2<T, TContext> DoNotRunOnAttach();
        IConditionalRuleBuilderPhase2<T, TContext> DoNotOverride();
        IConditionalRuleBuilderPhase2<T, TContext> OverrideKey(string bindingRuleKey);

        IConditionalRuleBuilderPhase2<T, TContext> Debug(Action<TContext> debugAction);

        void To(Expression<Func<TContext, T>> targetExpression);
        void To(Action<TContext, T> action);
    }

    public interface IConditionalRuleBuilderPhase1<T, TContext> : IConditionalRuleBuilderPhase2<T, TContext>
        where TContext : class
    {
        IConditionalRuleBuilderPhase1<T, TContext> ElseIf(Expression<Func<TContext, bool>> conditionalExpression, Expression<Func<TContext, T>> targetExpression);
        IConditionalRuleBuilderPhase2<T, TContext> Else(Expression<Func<TContext, T>> targetExpression);
    }

    internal sealed class ConditionalRuleBuilder<T, TContext> : IConditionalRuleBuilderPhase1<T, TContext>
        where TContext : class
    {
        private readonly Binder<TContext> _binder;
        private readonly List<Tuple<Expression, Expression, DebugContextBuilder>> _clauses = new List<Tuple<Expression, Expression, DebugContextBuilder>>();
        private readonly ParameterExpression _contextParameter;
        private bool _runOnAttach = true;
        private bool _canOverride = true;
        private bool _hasElseClause = false;
        private string _key;
        private Action<TContext> _debugAction;

        public ConditionalRuleBuilder(Binder<TContext> binder, Expression<Func<TContext, bool>> conditionalExpression, Expression<Func<TContext, T>> targetExpression)
        {
            _binder = binder;
            _contextParameter = Expression.Parameter(typeof (TContext));
            _clauses.Add(Tuple.Create(conditionalExpression.GetBodyWithReplacedParameter(_contextParameter), targetExpression.GetBodyWithReplacedParameter(_contextParameter), new DebugContextBuilder(targetExpression.Body, " (branch 0)")));
        }

        public IConditionalRuleBuilderPhase1<T, TContext> ElseIf(Expression<Func<TContext, bool>> conditionalExpression, Expression<Func<TContext, T>> targetExpression)
        {
            _clauses.Add(Tuple.Create(conditionalExpression.GetBodyWithReplacedParameter(_contextParameter), targetExpression.GetBodyWithReplacedParameter(_contextParameter), new DebugContextBuilder(targetExpression.Body, string.Format(" (branch {0})", _clauses.Count))));
            return this;
        }

        public IConditionalRuleBuilderPhase2<T, TContext> Else(Expression<Func<TContext, T>> targetExpression)
        {
            if (_hasElseClause)
            {
                throw new Exception("Current conditional binding already has an 'Else' clause");
            }
            _clauses.Add(Tuple.Create((Expression)null, targetExpression.GetBodyWithReplacedParameter(_contextParameter), new DebugContextBuilder(targetExpression.Body, string.Format(" (branch {0})", _clauses.Count))));
            _hasElseClause = true;
            return this;
        }

        public IConditionalRuleBuilderPhase2<T, TContext> OverrideKey(string bindingRuleKey)
        {
            _key = bindingRuleKey;
            return this;
        }

        public IConditionalRuleBuilderPhase2<T, TContext> Debug(Action<TContext> debugAction)
        {
            _debugAction = debugAction;
            return this;
        }

        public void To(Expression<Func<TContext, T>> targetExpression)
        {
            AddElseClauseIfNecessary();

            var targetBody = targetExpression.GetBodyWithReplacedParameter(_contextParameter);
            var key = _key ?? targetExpression.GetTargetKey();

            var targetParent = ((MemberExpression)targetExpression.Body).Expression;
            var targetParameter = targetExpression.Parameters[0];

            for (int i = 0; i < _clauses.Count; ++i)
            {
                var sourceExpression = _clauses[i].Item2;
                if (sourceExpression == null && _debugAction == null)
                {
                    break;
                }

                var conditionExpression = _clauses[i].Item1 == null
                    ? Expression.Not(CombineOr(_clauses.Take(i).Select(x => x.Item1)))
                    : i > 0
                        ? Expression.AndAlso(Expression.Not(CombineOr(_clauses.Take(i).Select(x => x.Item1))), _clauses[i].Item1)
                        : _clauses[i].Item1;

                var assignmentExpression = Expression.IfThen(
                    conditionExpression,
                    AddDebugAction(
                        sourceExpression == null ? null : Expression.Assign(
                            targetBody,
                            sourceExpression)));

                var assignment = Binder.ExpressionCompiler.Compile(
                    Expression.Lambda<Action<TContext>>(
                        assignmentExpression,
                        _contextParameter));

                var dependencies = new List<Expression> { conditionExpression, sourceExpression };
                if (targetParent != targetParameter)
                {
                    dependencies.Add(targetParent);
                }

                _binder.AddRule(assignment, key, _clauses[i].Item3.CreateContext(typeof(TContext).Name, key), _runOnAttach, i == 0 && _canOverride, dependencies);
            }
        }

        public void To(Action<TContext, T> action)
        {
            AddElseClauseIfNecessary();

            var actionParameter = Expression.Parameter(typeof(Action<TContext, T>));

            for (int i = 0; i < _clauses.Count; ++i)
            {
                var sourceExpression = _clauses[i].Item2;
                if (sourceExpression == null && _debugAction == null)
                {
                    break;
                }

                var innerExpression = AddDebugAction(
                    sourceExpression == null ? null : Expression.Invoke(
                        actionParameter,
                        _contextParameter,
                        sourceExpression));

                var conditionExpression = _clauses[i].Item1 == null
                    ? Expression.Not(CombineOr(_clauses.Take(i).Select(x => x.Item1))) 
                    : i > 0
                        ? Expression.AndAlso(Expression.Not(CombineOr(_clauses.Take(i).Select(x => x.Item1))), _clauses[i].Item1)
                        : _clauses[i].Item1;

                var invokeExpression = Expression.IfThen(conditionExpression, innerExpression);
                var invoke = Binder.ExpressionCompiler.Compile(Expression.Lambda<Action<TContext, Action<TContext, T>>>(invokeExpression, _contextParameter, actionParameter));

                _binder.AddRule(ctx =>
                    {
                        invoke(ctx, action);
                    }, _key, _clauses[i].Item3.CreateContext(typeof(TContext).Name, _key), _runOnAttach, i == 0 && _canOverride, new[] { invokeExpression });
            }
        }

        public IConditionalRuleBuilderPhase2<T, TContext> DoNotRunOnAttach()
        {
            _runOnAttach = false;
            return this;
        }

        public IConditionalRuleBuilderPhase2<T, TContext> DoNotOverride()
        {
            _canOverride = false;
            return this;
        }

        private Expression CombineOr(IEnumerable<Expression> expressions)
        {
            return expressions.Aggregate<Expression, Expression>(null, (current, e) => current == null ? e : Expression.OrElse(current, e));
        }

        private void AddElseClauseIfNecessary()
        {
            if (_debugAction != null && !_hasElseClause)
            {
                _clauses.Add(Tuple.Create((Expression)null, (Expression)null, new DebugContextBuilder(Expression.Empty(), "(no branch)")));
                _hasElseClause = true;
            }
        }

        private Expression AddDebugAction(Expression expression)
        {
            var debugExpression = _debugAction == null ? null : Expression.Invoke(
                Expression.Constant(_debugAction),
                _contextParameter);

            return ExpressionHelpers.CombineToBlock(debugExpression, expression);
        }
    }
}