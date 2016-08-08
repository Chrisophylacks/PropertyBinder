using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    public interface IConditionalRuleBuilderPhase2<T, TContext>
        where TContext : class
    {
        IConditionalRuleBuilderPhase2<T, TContext> DoNotRunOnAttach();
        IConditionalRuleBuilderPhase2<T, TContext> DoNotOverride();
        IConditionalRuleBuilderPhase2<T, TContext> OverrideKey(string bindingRuleKey);

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
        private readonly PropertyBinder<TContext> _binder;
        private readonly List<Tuple<Expression, Expression>> _clauses = new List<Tuple<Expression, Expression>>();
        private Expression _defaultExpression;
        private readonly ParameterExpression _contextParameter;
        private bool _runOnAttach = true;
        private bool _canOverride = true;
        private string _key;

        public ConditionalRuleBuilder(PropertyBinder<TContext> binder)
        {
            _binder = binder;
            _contextParameter = Expression.Parameter(typeof (TContext));
        }

        public IConditionalRuleBuilderPhase1<T, TContext> ElseIf(Expression<Func<TContext, bool>> conditionalExpression, Expression<Func<TContext, T>> targetExpression)
        {
            _clauses.Add(Tuple.Create(conditionalExpression.GetBodyWithReplacedParameter(_contextParameter), targetExpression.GetBodyWithReplacedParameter(_contextParameter)));
            return this;
        }

        public IConditionalRuleBuilderPhase2<T, TContext> Else(Expression<Func<TContext, T>> targetExpression)
        {
            _defaultExpression = targetExpression.GetBodyWithReplacedParameter(_contextParameter);
            return this;
        }

        public IConditionalRuleBuilderPhase2<T, TContext> OverrideKey(string bindingRuleKey)
        {
            _key = bindingRuleKey;
            return this;
        }

        public void To(Expression<Func<TContext, T>> targetExpression)
        {
            var targetBody = targetExpression.GetBodyWithReplacedParameter(_contextParameter);
            var key = _key ?? targetExpression.GetTargetKey();

            var targetParent = ((MemberExpression)targetExpression.Body).Expression;
            var targetParameter = targetExpression.Parameters[0];

            for (int i = 0; i <= _clauses.Count; ++i)
            {
                var sourceExpression = i == _clauses.Count ? _defaultExpression : _clauses[i].Item2;
                if (sourceExpression == null)
                {
                    break;
                }

                var conditionExpression = i == _clauses.Count
                    ? Expression.Not(CombineOr(_clauses.Select(x => x.Item1)))
                    : i > 0
                        ? Expression.AndAlso(Expression.Not(CombineOr(_clauses.Take(i).Select(x => x.Item1))), _clauses[i].Item1)
                        : _clauses[i].Item1;

                var assignmentExpression = Expression.IfThen(
                    conditionExpression, 
                    Expression.Assign(
                        targetBody,
                        sourceExpression));

                var assignment = Expression.Lambda<Action<TContext>>(
                    assignmentExpression,
                    _contextParameter).Compile();

                var dependencies = new List<Expression> { conditionExpression, sourceExpression };
                if (targetParent != targetParameter)
                {
                    dependencies.Add(targetParent);
                }

                _binder.AddRule(assignment, key, _runOnAttach, i == 0 && _canOverride, dependencies);
            }
        }

        public void To(Action<TContext, T> action)
        {
            var actionParameter = Expression.Parameter(typeof(Action<TContext, T>));

            for (int i = 0; i <= _clauses.Count; ++i)
            {
                var sourceExpression = i == _clauses.Count ? _defaultExpression : _clauses[i].Item2;
                if (sourceExpression == null)
                {
                    break;
                }

                var innerExpression = Expression.Invoke(
                    actionParameter,
                    _contextParameter,
                    sourceExpression);

                var conditionExpression = i == _clauses.Count
                    ? Expression.Not(CombineOr(_clauses.Select(x => x.Item1))) 
                    : i > 0
                        ? Expression.AndAlso(Expression.Not(CombineOr(_clauses.Take(i).Select(x => x.Item1))), _clauses[i].Item1)
                        : _clauses[i].Item1;

                var invokeExpression = Expression.IfThen(conditionExpression, innerExpression);
                var invoke = Expression.Lambda<Action<TContext, Action<TContext, T>>>(invokeExpression, _contextParameter, actionParameter).Compile();

                _binder.AddRule(ctx => invoke(ctx, action), _key, _runOnAttach, false, new[] { invokeExpression });
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
    }
}