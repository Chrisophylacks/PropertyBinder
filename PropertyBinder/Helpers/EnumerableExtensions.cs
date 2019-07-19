using System;
using System.Collections.Generic;
using System.Linq;

namespace PropertyBinder.Helpers
{
    internal static class EnumerableExtensions
    {
        public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TSource, TKey, TValue>(this ICollection<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        {
            if (source.Count == 1)
            {
                var item = source.ElementAt(0);
                return new SingleElementDictionary<TKey, TValue>(keySelector(item), valueSelector(item));
            }

            return source.ToDictionary(keySelector, valueSelector);
        }
    }
}