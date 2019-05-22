using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using PropertyBinder.Engine;
using PropertyBinder.Visitors;

namespace PropertyBinder.Helpers
{
    internal static class ExpressionHelpers
    {
        public static List<BindableMember> GetPathToParameter(this Expression node, Type parameterType)
        {
            var list = new List<BindableMember>();
            var expr = node;
            while (true)
            {
                if (expr == null)
                {
                    return null;
                }

                if (expr.NodeType == ExpressionType.Parameter && expr.Type == parameterType)
                {
                    list.Reverse();
                    return list;
                }

                var memberExpr = expr as MemberExpression;
                if (memberExpr != null)
                {
                    var member = memberExpr.Member;

                    if (member is PropertyInfo)
                    {
                        list.Add(new BindableMember((PropertyInfo) member));
                    }
                    else if (member is FieldInfo)
                    {
                        list.Add(new BindableMember((FieldInfo) member));
                    }

                    expr = memberExpr.Expression;
                    continue;
                }

                if (expr.NodeType == ExpressionType.Convert || expr.NodeType == ExpressionType.ConvertChecked)
                {
                    var unary = expr as UnaryExpression;
                    if (unary != null)
                    {
                        expr = unary.Operand;
                        continue;
                    }
                }

                // attempt to resolve path from indexer
                var callExpr = expr as MethodCallExpression;
                string index;
                if (callExpr != null && callExpr.IsBindableIndexerInvocation(out index))
                {
                    list.Add(new BindableMember(index));
                    expr = callExpr.Object;
                    continue;
                }

                return null;
            }
        }

        public static bool IsBindableIndexerInvocation(this MethodCallExpression callExpr, out string index)
        {
            if (callExpr.Method.IsSpecialName && callExpr.Method.Name == "get_Item" && callExpr.Arguments.Count == 1)
            {
                var indexArg = callExpr.Arguments[0];
                if (indexArg.Type == typeof (string) && indexArg.NodeType == ExpressionType.Constant)
                {
                    index = (string) ((ConstantExpression) indexArg).Value;
                    return true;
                }
            }

            index = null;
            return false;
        }

        public static string GetTargetKey<TContext, T>(this Expression<Func<TContext, T>> targetExpression)
        {
            var body = targetExpression.Body as MemberExpression;
            if (body == null)
            {
                throw new ArgumentOutOfRangeException("targetExpression", "Target expression body must a member expression");
            }

            var path = body.GetPathToParameter(targetExpression.Parameters[0].Type);
            if (path == null)
            {
                throw new ArgumentOutOfRangeException("targetExpression", "Target expression body must be contain only member expressions");
            }

            return string.Join(".", path.Select(x => x.Name).ToArray());
        }

        public static Expression GetBodyWithReplacedParameter<TContext, T>(this Expression<Func<TContext, T>> source, ParameterExpression parameter)
        {
            return new ReplaceParameterVisitor(source.Parameters[0], parameter).Visit(source.Body);
        }

        public static Expression CombineToBlock(params Expression[] expressions)
        {
            var filteredExpressions = expressions.Where(x => x != null).ToArray();
            if (filteredExpressions.Length == 0)
            {
                return Expression.Empty();
            }

            if (filteredExpressions.Length == 1)
            {
                return filteredExpressions[0];
            }

            return Expression.Block(filteredExpressions);
        }
    }
}
