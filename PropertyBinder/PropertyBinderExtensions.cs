using System;
using System.Linq.Expressions;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    public static class PropertyBinderExtensions
    {
        public static PropertyRuleBuilder<T, TContext> Bind<T, TContext>(this Binder<TContext> binder, Expression<Func<TContext, T>> sourceExpression)
            where TContext : class
        {
            return new PropertyRuleBuilder<T, TContext>(binder, sourceExpression);
        }

        public static CommandRuleBinder<TContext> BindCommand<TContext>(this Binder<TContext> binder, Action<TContext> executeAction, Expression<Func<TContext, bool>> canExecuteExpression)
            where TContext : class
        {
            return new CommandRuleBinder<TContext>(binder, executeAction, canExecuteExpression);
        }

        public static void Unbind<T, TContext>(this Binder<TContext> binder, Expression<Func<TContext, T>> targetExpression)
            where TContext : class
        {
            binder.Rules.RemoveRule(targetExpression.GetTargetKey());
        }

        public static void Unbind<TContext>(this Binder<TContext> binder, string bindingRuleKey)
            where TContext : class
        {
            binder.Rules.RemoveRule(bindingRuleKey);
        }

        public static IConditionalRuleBuilderPhase1<T, TContext> BindIf<T, TContext>(this Binder<TContext> binder, Expression<Func<TContext, bool>> conditionalExpression, Expression<Func<TContext, T>> targetExpression)
            where TContext : class
        {
            return new ConditionalRuleBuilder<T, TContext>(binder).ElseIf(conditionalExpression, targetExpression);
        }

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
        }

        public static T OverrideKey<T>(this T rule, string bindingRuleKey)
            where T : BindingRuleBase
        {
            rule.SetRuleKey(bindingRuleKey);
            return rule;
        }

        public static T DoNotOverride<T>(this T rule)
            where T : BindingRuleBase
        {
            rule.DoNotOverride();
            return rule;
        }

        public static T DoNotRunOnAttach<T>(this T rule)
            where T : BindingRuleBase
        {
            rule.DoNotRunOnAttach();
            return rule;
        }
    }
}
