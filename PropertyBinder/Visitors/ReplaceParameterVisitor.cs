using System.Linq.Expressions;

namespace PropertyBinder.Visitors
{
    internal sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _source)
            {
                return _target;
            }

            return base.VisitParameter(node);
        }
    }
}
