using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using PropertyBinder.Helpers;
using PropertyBinder.Visitors;

namespace PropertyBinder
{
    public sealed class PropertyRuleBuilder<T, TContext>
        where TContext : class
    {
        private readonly PropertyBinder<TContext> _binder;
        private readonly Expression<Func<TContext, T>> _sourceExpression;
        private readonly List<LambdaExpression> _dependencies = new List<LambdaExpression>();
        private bool _runOnAttach = true;
        private bool _canOverride = true;

        internal PropertyRuleBuilder(PropertyBinder<TContext> binder, Expression<Func<TContext, T>> sourceExpression)
        {
            _binder = binder;
            _sourceExpression = sourceExpression;
            _dependencies.Add(_sourceExpression);
        }

        public void To(Expression<Func<TContext, T>> targetExpression)
        {
            var contextParameter = _sourceExpression.Parameters[0];

            var assignment = Expression.Lambda<Action<TContext>>(
                Expression.Assign(
                    new ReplaceParameterVisitor(targetExpression.Parameters[0], contextParameter).Visit(targetExpression.Body),
                    _sourceExpression.Body),
                contextParameter);

            var key = targetExpression.GetTargetKey();

            var targetParent = ((MemberExpression) targetExpression.Body).Expression;
            var targetParameter = targetExpression.Parameters[0];
            if (targetParent != targetParameter)
            {
                _dependencies.Add(Expression.Lambda(targetParent, targetParameter));
            }

            AddRule(assignment.Compile(), key);
        }

        public void To(Action<TContext, T> action)
        {
            var getValue = _sourceExpression.Compile();
            AddRule(ctx => action(ctx, getValue(ctx)), null);
        }

        public void To(Action<TContext> action)
        {
            AddRule(action, null);
        }

        public PropertyRuleBuilder<T, TContext> DoNotRunOnAttach()
        {
            _runOnAttach = false;
            return this;
        }

        public PropertyRuleBuilder<T, TContext> DoNotOverride()
        {
            _canOverride = false;
            return this;
        }

        public PropertyRuleBuilder<T, TContext> WithDependency<TDependency>(Expression<Func<TContext, TDependency>> dependencyExpression)
        {
            _dependencies.Add(dependencyExpression);
            return this;
        }

        private void AddRule(Action<TContext> action, string key)
        {
            _binder.AddRule(action, key, _runOnAttach, _canOverride, _dependencies);
        }
    }
}