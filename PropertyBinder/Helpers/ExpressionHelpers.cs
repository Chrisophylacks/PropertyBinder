﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using PropertyBinder.Visitors;

namespace PropertyBinder.Helpers
{
    internal static class ExpressionHelpers
    {
        public static List<MemberInfo> GetPathToParameter(this Expression node, Type parameterType)
        {
            var list = new List<MemberInfo>();
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

                var member = expr as MemberExpression;
                if (member == null)
                {
                    return null;
                }

                list.Add(member.Member);
                expr = member.Expression;
            }
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
    }
}
