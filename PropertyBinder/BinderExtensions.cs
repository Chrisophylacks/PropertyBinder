using System;
using System.Diagnostics;
using System.Linq.Expressions;
using PropertyBinder.Decompiler;
using PropertyBinder.Diagnostics;
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
            var parameter = Expression.Parameter(typeof (object), "parameter");
            var canExecuteWithParameter = Expression.Lambda<Func<TContext, object, bool>>(
                canExecuteExpression.Body,
                canExecuteExpression.Parameters[0],
                parameter);

            return new CommandRuleBinder<TContext>(binder, (ctx, _) => executeAction(ctx), canExecuteWithParameter, false);
        }

        public static CommandRuleBinder<TContext> BindCommand<TContext>(this Binder<TContext> binder, Action<TContext, object> executeAction, Expression<Func<TContext, object, bool>> canExecuteExpression)
            where TContext : class
        {
            return new CommandRuleBinder<TContext>(binder, executeAction, canExecuteExpression, true);
        }

        public static void Unbind<T, TContext>(this Binder<TContext> binder, Expression<Func<TContext, T>> targetExpression)
            where TContext : class
        {
            binder.RemoveRule(targetExpression.GetTargetKey());
        }

        public static void Unbind<TContext>(this Binder<TContext> binder, string key)
            where TContext : class
        {
            binder.RemoveRule(key);
        }

        public static IConditionalRuleBuilderPhase1<T, TContext> BindIf<T, TContext>(this Binder<TContext> binder, Expression<Func<TContext, bool>> conditionalExpression, Expression<Func<TContext, T>> targetExpression)
            where TContext : class
        {
            return new ConditionalRuleBuilder<T, TContext>(binder, conditionalExpression, targetExpression);
        }

        public static void BindAction<TContext>(this Binder<TContext> binder, Expression<Action<TContext>> expression, string overrideKey = null)
            where TContext : class
        {
            binder.AddRule(expression.Compile(), overrideKey, new DebugContextBuilder(expression.Body, null).CreateContext(typeof(TContext).Name, overrideKey), true, !string.IsNullOrEmpty(overrideKey), null, new Expression[] {expression});
        }

        public static void AddRule<TContext>(this Binder<TContext> binder, Action<TContext> bindingAction, string key, string debugDescription, bool runOnAttach, bool canOverride, Expression stampExpression, params Expression[] triggerExpressions)
            where TContext : class
        {
            binder.AddRule(bindingAction, key, new DebugContextBuilder(debugDescription).CreateContext(typeof(TContext).Name, key), runOnAttach, canOverride, stampExpression, triggerExpressions);
        }

        internal static void BindEvent<TContext>(this Binder<TContext> binder, Action<TContext> eventSubscription)
            where TContext : class
        {
            Expression left;
            Expression right;
            Action<TContext> unsubscribe;
            MethodAnalyzer.SplitEventExpression(eventSubscription, out left, out right, out unsubscribe);
            binder.AddRule(null, null, null, true, false, null, new[] { left, right });
        }
    }
}