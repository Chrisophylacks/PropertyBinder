using System;
using System.Linq.Expressions;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    public static class PropertyBinderExtensions
    {
        [Obsolete]
        public static PropertyRuleBuilder<T, TContext> Bind<T, TContext>(this PropertyBinder<TContext> binder, Expression<Func<TContext, T>> sourceExpression)
            where TContext : class
        {
            return new PropertyRuleBuilder<T, TContext>(binder.Binder, sourceExpression);
        }

        [Obsolete]
        public static CommandRuleBinder<TContext> BindCommand<TContext>(this PropertyBinder<TContext> binder, Action<TContext> executeAction, Expression<Func<TContext, bool>> canExecuteExpression)
            where TContext : class
        {
            return new CommandRuleBinder<TContext>(binder.Binder, executeAction, canExecuteExpression);
        }

        [Obsolete]
        public static void Unbind<T, TContext>(this PropertyBinder<TContext> binder, Expression<Func<TContext, T>> targetExpression)
            where TContext : class
        {
            binder.Binder.RemoveRule(targetExpression.GetTargetKey());
        }

        [Obsolete]
        public static IConditionalRuleBuilderPhase1<T, TContext> BindIf<T, TContext>(this PropertyBinder<TContext> binder, Expression<Func<TContext, bool>> conditionalExpression, Expression<Func<TContext, T>> targetExpression)
            where TContext : class
        {
            return new ConditionalRuleBuilder<T, TContext>(binder.Binder, conditionalExpression, targetExpression);
        }

        public static PropertyRuleBuilder<T, TContext> PropagateNullValues<T, TContext>(this PropertyRuleBuilder<T, TContext> ruleBuilder)
            where T : class
            where TContext : class
        {
            ruleBuilder.SetPropagateNullValues(true);
            return ruleBuilder;
        }

        public static PropertyRuleBuilder<T?, TContext> PropagateNullValues<T, TContext>(this PropertyRuleBuilder<T?, TContext> ruleBuilder)
            where T : struct
            where TContext : class
        {
            ruleBuilder.SetPropagateNullValues(true);
            return ruleBuilder;
        }

        public static void To<T, TContext, TTarget>(this PropertyRuleBuilder<T, TContext> ruleBuilder, Expression<Func<TContext, TTarget>> targetExpression)
            where TContext : class
            where T : TTarget
        {
            ruleBuilder.SetTarget(targetExpression);
        }

        public static void To<T, TContext>(this PropertyRuleBuilder<T, TContext> ruleBuilder, Expression<Func<TContext, T?>> targetExpression)
            where T : struct
            where TContext : class
        {
            ruleBuilder.SetTarget(targetExpression);
        }
    }
}
