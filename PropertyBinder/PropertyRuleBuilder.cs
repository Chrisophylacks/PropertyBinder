﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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

        internal PropertyRuleBuilder(Binder<TContext> binder, Expression<Func<TContext, T>> sourceExpression)
        {
            _binder = binder;
            _sourceExpression = sourceExpression;
            _dependencies.Add(_sourceExpression.Body);
        }

        public void To(Expression<Func<TContext, T>> targetExpression)
        {
            var contextParameter = _sourceExpression.Parameters[0];

            var source = _sourceExpression.Body;
            if (_propagateNullValues)
            {
                source = new NullPropagationVisitor(_sourceExpression.Parameters[0]).Visit(source);
            }

            var assignment = Expression.Lambda<Action<TContext>>(
                Expression.Assign(
                    targetExpression.GetBodyWithReplacedParameter(contextParameter),
                    source),
                contextParameter);

            var key = _key ?? targetExpression.GetTargetKey();

            var targetParent = ((MemberExpression) targetExpression.Body).Expression;
            var targetParameter = targetExpression.Parameters[0];
            if (targetParent != targetParameter)
            {
                _dependencies.Add(targetParent);
            }

            AddRule(assignment.Compile(), key);
        }

        public void To(Action<TContext, T> action)
        {
            Func<TContext, T> getValue;
            if (_propagateNullValues)
            {
                getValue = Expression.Lambda<Func<TContext, T>>(
                    new NullPropagationVisitor(_sourceExpression.Parameters[0]).Visit(_sourceExpression.Body),
                    _sourceExpression.Parameters[0])
                    .Compile();
            }
            else
            {
                getValue = _sourceExpression.Compile();
            }

            AddRule(ctx => action(ctx, getValue(ctx)), _key);
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
            _binder.AddRule(_debugAction == null ? action : _debugAction + action, key, _runOnAttach, _canOverride, _dependencies);
        }
    }
}