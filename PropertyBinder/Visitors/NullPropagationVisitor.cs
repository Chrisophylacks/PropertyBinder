using System;
using System.Linq.Expressions;

namespace PropertyBinder.Visitors
{
    internal sealed class NullPropagationVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _rootParameter;
        private PropagationPoint _propagationPoint;

        public NullPropagationVisitor(ParameterExpression rootParameter)
        {
            _rootParameter = rootParameter;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == _rootParameter || node.Expression == null || !IsNullable(node.Expression.Type))
            {
                return base.VisitMember(node);
            }

            var temp = Expression.Variable(node.Expression.Type);
            
            return Expression.Block(
                new [] { temp },               
                Expression.Assign(temp, base.Visit(node.Expression)),
                Expression.IfThen(
                    Expression.Equal(temp, Expression.Constant(null, node.Expression.Type)),
                    Expression.Return(_propagationPoint.Label)),
                Expression.MakeMemberAccess(Expression.Convert(temp, node.Expression.Type), node.Member));
        }


        public override Expression Visit(Expression node)
        {
            if (node == null || !IsNullable(node.Type))
            {
                return base.Visit(node);
            }

            var oldPropagationPoint = _propagationPoint;
            _propagationPoint = new PropagationPoint();
            
            var transformedExpression = base.Visit(node);

            if (_propagationPoint.IsActive)
            {
                var result = Expression.Variable(node.Type);

                transformedExpression = Expression.Block(
                    new[] { result }, 
                    Expression.Assign(result, Expression.Constant(null, node.Type)),
                    Expression.Assign(result, transformedExpression),
                    Expression.Label(_propagationPoint.Label),
                    result);
            }

            _propagationPoint = oldPropagationPoint;
            return transformedExpression;
        }

        private bool IsNullable(Type type)
        {
            if (type.IsClass)
            {
                return true;
            }

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private sealed class PropagationPoint
        {
            private LabelTarget _label;

            public bool IsActive
            {
                get { return _label != null; }
            }

            public LabelTarget Label
            {
                get { return _label ?? (_label = Expression.Label()); }
            }
        }
    }
}