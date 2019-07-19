using System;
using System.Linq.Expressions;

namespace PropertyBinder
{
    public interface IExpressionCompiler
    {
        T Compile<T>(Expression<T> expression);

        Delegate Compile(LambdaExpression expression);
    }

    public sealed class DefaultExpressionCompiler : IExpressionCompiler
    {
        public static IExpressionCompiler Instance = new DefaultExpressionCompiler();

        private DefaultExpressionCompiler() { }

        public T Compile<T>(Expression<T> expression)
        {
            return expression.Compile();
        }

        public Delegate Compile(LambdaExpression expression)
        {
            return expression.Compile();
        }
    }
}
