using System;
using System.Linq.Expressions;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    public static class PropertyBinderExtensions
    {
        public static PropertyRuleBuilder<T, TContext> Bind<T, TContext>(this PropertyBinder<TContext> binder, Expression<Func<TContext, T>> sourceExpression)
            where TContext : class
        {
            return new PropertyRuleBuilder<T, TContext>(binder, sourceExpression);
        }

        public static CommandRuleBinder<TContext> BindCommand<TContext>(this PropertyBinder<TContext> binder, Action<TContext> executeAction, Expression<Func<TContext, bool>> canExecuteExpression)
            where TContext : class
        {
            return new CommandRuleBinder<TContext>(binder, executeAction, canExecuteExpression);
        }

        public static void Unbind<T, TContext>(this PropertyBinder<TContext> binder, Expression<Func<TContext, T>> targetExpression)
            where TContext : class
        {
            binder.RemoveRule(targetExpression.GetTargetKey());
        }

        public static IConditionalRuleBuilderPhase1<T, TContext> BindIf<T, TContext>(this PropertyBinder<TContext> binder, Expression<Func<TContext, bool>> conditionalExpression, Expression<Func<TContext, T>> targetExpression)
            where TContext : class
        {
            return new ConditionalRuleBuilder<T, TContext>(binder).ElseIf(conditionalExpression, targetExpression);
        }

        /*
        public static PropertyRuleBuilder<T, TContext> PropagateNullValues<T, TContext>(this PropertyRuleBuilder<T, TContext> ruleBuilder)
            where T : class
            where TContext : class
        {
            ruleBuilder.PropagateNullValues();
            return ruleBuilder;
        }

        public static PropertyRuleBuilder<T?, TContext> PropagateNullValues<T, TContext>(this PropertyRuleBuilder<T?, TContext> ruleBuilder)
            where T : struct
            where TContext : class
        {
            ruleBuilder.PropagateNullValues();
            return ruleBuilder;
        }*/
    }
}
