using System;
using System.Linq.Expressions;
using PropertyBinder.Decompiler;
using PropertyBinder.Helpers;

namespace PropertyBinder
{
    public static class BinderExtensions
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
            binder.RemoveRule(targetExpression.GetTargetKey());
        }

        public static IConditionalRuleBuilderPhase1<T, TContext> BindIf<T, TContext>(this Binder<TContext> binder, Expression<Func<TContext, bool>> conditionalExpression, Expression<Func<TContext, T>> targetExpression)
            where TContext : class
        {
            return new ConditionalRuleBuilder<T, TContext>(binder).ElseIf(conditionalExpression, targetExpression);
        }

        internal static void BindEvent<TContext>(this Binder<TContext> binder, Action<TContext> eventSubscription)
            where TContext : class
        {
            Expression left;
            Expression right;
            Action<TContext> unsubscribe;
            MethodAnalyzer.SplitEventExpression(eventSubscription, out left, out right, out unsubscribe);
            binder.AddRule(null, null, true, false, new[] { left, right });
        }
    }
}