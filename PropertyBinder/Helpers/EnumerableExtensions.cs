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

        internal static TResult[] CompactSelect<T, TResult>(this T[] source, ICollection<int> indexes)
            where T : TResult
        {
            var res = new TResult[indexes.Count];
            int j = 0;
            for (int i = 0; i < res.Length; ++i)
            {
                var value = source[indexes.ElementAt(i)];
                if (value != null)
                {
                    res[j++] = value;
                }
            }

            if (j != res.Length)
            {
                Array.Resize(ref res, j);
            }

            return res;
        }
    }
}