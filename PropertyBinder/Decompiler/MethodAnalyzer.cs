using System;
using System.Linq;
using System.Linq.Expressions;

namespace PropertyBinder.Decompiler
{
    internal static class MethodAnalyzer
    {
        public static void SplitEventExpression<T>(Action<T> method, out Expression left, out Expression right, out Action<T> unsubscribe)
            where T : class
        {
            var ops = new IlReader(method.Method).ReadToEnd().ToList();
            left = null;
            right = null;
            unsubscribe = null;
        }
    }
}
