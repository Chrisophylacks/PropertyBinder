using System;
using System.Linq.Expressions;
using PropertyBinder.Engine;
using PropertyBinder.Helpers;

namespace PropertyBinder.Visitors
{
    internal sealed class BindingExpressionVisitor<TContext> : ExpressionVisitor
        where TContext : class
    {
        private readonly IBindingNode<TContext> _rootNode;
        private readonly Type _rootParameterType;
        private readonly Action<TContext> _bindingAction;

        public BindingExpressionVisitor(IBindingNode<TContext> rootNode, Type rootParameterType, Action<TContext> bindingAction)
        {
            _rootNode = rootNode;
            _rootParameterType = rootParameterType;
            _bindingAction = bindingAction;
        }

        protected override Expression VisitMember(MemberExpression expr)
        {
            // accessors
            if (TryBindPath(expr))
            {
                return expr;
            }

            return base.VisitMember(expr);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expr)
        {
            // indexers
            if (TryBindPath(expr))
            {
                return expr;
            }

            // collection aggregates
            foreach (var arg in expr.Arguments)
            {
                var collectionItemType = arg.Type.ResolveCollectionItemType();
                if (collectionItemType == null)
                {
                    continue;
                }

                var path = arg.GetPathToParameter(_rootParameterType);
                if (path == null)
                {
                    continue;
                }

                var node = _rootNode;
                foreach (var entry in path)
                {
                    node = node.GetSubNode(entry);
                }

                var collectionNode = node.GetCollectionNode(collectionItemType);
                if (collectionNode == null)
                {
                    continue;
                }

                collectionNode.AddAction(_bindingAction);

                BindingExpressionVisitor<TContext> itemVisitor = null;
                foreach (var arg2 in expr.Arguments)
                {
                    if (arg2.NodeType == ExpressionType.Lambda)
                    {
                        var lambda = (LambdaExpression) arg2;
                        if (lambda.Parameters.Count == 1 && lambda.Parameters[0].Type.IsAssignableFrom(collectionItemType))
                        {
                            if (itemVisitor == null)
                            {
                                itemVisitor = new BindingExpressionVisitor<TContext>(collectionNode.GetItemNode(), collectionItemType, _bindingAction);
                            }
                            itemVisitor.Visit(lambda.Body);
                        }
                    }
                }
            }

            return base.VisitMethodCall(expr);
        }

        private bool TryBindPath(Expression expr)
        {
            var path = expr.GetPathToParameter(_rootParameterType);
            if (path != null)
            {
                var node = _rootNode;
                BindableMember parentMember = null;

                foreach (var entry in path)
                {
                    if (parentMember != null)
                    {
                        node = node.GetSubNode(parentMember);
                    }

                    if (entry.CanSubscribe)
                    {
                        node.AddAction(entry.Name, _bindingAction);
                    }

                    parentMember = entry;
                }

                return true;
            }

            return false;
        }
    }
}
