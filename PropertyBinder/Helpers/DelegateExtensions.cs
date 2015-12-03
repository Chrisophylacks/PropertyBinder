using System;
using System.Linq;

namespace PropertyBinder.Helpers
{
    internal static class DelegateExtensions
    {
        public static T CombineUnique<T>(this T first, T second)
            where T : class
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            var d1 = (Delegate) (object) first;
            var d2 = (Delegate) (object) second;

            var invocations = d1.GetInvocationList().Union(d2.GetInvocationList()).Distinct(ReferenceEqualityComparer<Delegate>.Instance).ToArray();
            return (T)(object)Delegate.Combine(invocations);
        }

        public static T RemoveUnique<T>(this T first, T second)
            where T : class
        {
            if (first == null)
            {
                return null;
            }

            if (second == null)
            {
                return first;
            }

            var d1 = (Delegate)(object)first;
            var d2 = (Delegate)(object)second;

            var invocations = d1.GetInvocationList().Except(d2.GetInvocationList()).ToArray();
            if (invocations.Length == 0)
            {
                return null;
            }

            return (T)(object)Delegate.Combine(invocations);
        }
    }
}
