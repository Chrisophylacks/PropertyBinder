using System;
using System.Linq.Expressions;

namespace PropertyBinder
{
    public static class PropertyBinderExtensions
    {
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
