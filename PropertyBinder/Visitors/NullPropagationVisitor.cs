using System.Linq.Expressions;

namespace PropertyBinder.Visitors
{
    internal sealed class NullPropagationVisitor : ExpressionVisitor
    {
        private ParameterExpression _temp;
        private ParameterExpression _result;
        private LabelTarget _label;

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression.Type.IsValueType)
            {
                return base.VisitMember(node);
            }

            return Expression.Block(
                node.Type,
                Expression.Assign(_temp, base.Visit(node.Expression)),
                Expression.IfThen(
                    Expression.Equal(_temp, Expression.Constant(null)),
                    Expression.Return(_label)),
                Expression.MakeMemberAccess(Expression.Convert(_temp, node.Expression.Type), node.Member));
        }

        public new Expression Visit(Expression node)
        {
            _label = Expression.Label();
            _temp = Expression.Variable(typeof (object));
            _result = Expression.Variable(node.Type);

            return Expression.Block(
                node.Type,
                new[] { _temp, _result },
                Expression.Assign(_result, base.Visit(node)),
                Expression.Label(_label),
                _result);
        }
    }
}