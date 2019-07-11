using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FastExpressionCompiler;
using PropertyBinder.Diagnostics;
using PropertyBinder.Helpers;
using PropertyBinder.Visitors;

namespace PropertyBinder
{
    public sealed class PropertyRuleBuilder<T, TContext>
        where TContext : class
    {
        private readonly Binder<TContext> _binder;
        private readonly Expression<Func<TContext, T>> _sourceExpression;
        private readonly List<Expression> _dependencies = new List<Expression>();
        private bool _runOnAttach = true;
        private bool _canOverride = true;
        private bool _propagateNullValues;
        private Action<TContext> _debugAction;
        private string _key;
        private readonly DebugContextBuilder _debugContext;

        internal PropertyRuleBuilder(Binder<TContext> binder, Expression<Func<TContext, T>> sourceExpression)
        {
            _binder = binder;
            _sourceExpression = sourceExpression;
            _debugContext = new DebugContextBuilder(sourceExpression.Body, null);
            _dependencies.Add(_sourceExpression.Body);
        }

        // TODO: remove this method completely, it doesn't guarantee type safety at compile time. Unfortunately, that would be a breaking change...
        public void To(Expression<Func<TContext, T>> targetExpression)
        {
            SetTarget(targetExpression);
        }

        internal void SetTarget<TTarget>(Expression<Func<TContext, TTarget>> targetExpression)
        {
            var contextParameter = _sourceExpression.Parameters[0];

            var source = _sourceExpression.Body;
            if (_propagateNullValues)
            {
                source = new NullPropagationVisitor(_sourceExpression.Parameters[0]).Visit(source);
            }

            var target = targetExpression.GetBodyWithReplacedParameter(contextParameter);
            if (!target.Type.IsAssignableFrom(source.Type) || source.Type.IsValueType && Nullable.GetUnderlyingType(target.Type) == source.Type)
            {
                source = Expression.Convert(source, target.Type);
            }

            var assignment = Expression.Lambda<Action<TContext>>(
                Expression.Assign(
                    target,
                    source),
                contextParameter);

            var key = _key ?? targetExpression.GetTargetKey();

            var targetParent = ((MemberExpression) targetExpression.Body).Expression;
            var targetParameter = targetExpression.Parameters[0];
            if (targetParent != targetParameter)
            {
                _dependencies.Add(targetParent);
            }

            AddRule(assignment.CompileFast(), key);
        }

        public void To(Action<TContext, T> action)
        {
            Expression getValueExpression = _sourceExpression.Body;
            var contextParameter = _sourceExpression.Parameters[0];
            if (_propagateNullValues)
            {
                getValueExpression = new NullPropagationVisitor(contextParameter).Visit(_sourceExpression.Body);
            }

            var finalExpression = Expression.Lambda<Action<TContext>>(
                Expression.Call(
                    Expression.Constant(action),
                    typeof (Action<TContext, T>).GetMethod("Invoke"),
                    contextParameter,
                    getValueExpression),
                contextParameter);

            AddRule(finalExpression.CompileFast(), _key);
        }

        public void To(Action<TContext> action)
        {
            AddRule(action, _key);
        }

        public PropertyRuleBuilder<T, TContext> OverrideKey(string bindingRuleKey)
        {
            _key = bindingRuleKey;
            return this;
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

        public PropertyRuleBuilder<T, TContext> Debug(Action<TContext> debugAction)
        {
            _debugAction = debugAction;
            return this;
        }

        public PropertyRuleBuilder<T, TContext> WithDependency<TDependency>(Expression<Func<TContext, TDependency>> dependencyExpression)
        {
            _dependencies.Add(dependencyExpression.Body);
            return this;
        }

        internal void SetPropagateNullValues(bool value)
        {
            _propagateNullValues = value;
        }

        private void AddRule(Action<TContext> action, string key)
        {
            _binder.AddRule(_debugAction == null ? action : _debugAction + action, key, _debugContext.CreateContext(typeof(TContext).Name, key), _runOnAttach, _canOverride, _dependencies);
        }
    }
}