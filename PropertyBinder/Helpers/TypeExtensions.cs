using System;
using System.Collections;
using System.Collections.Generic;

namespace PropertyBinder.Helpers
{
    internal static class TypeExtensions
    {
        public static Type ResolveCollectionItemType(this Type collectionType)
        {
            if (!collectionType.IsValueType && collectionType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(collectionType))
            {
                if (collectionType.IsArray)
                {
                    return collectionType.GetElementType();
                }

                foreach (var ifc in collectionType.GetInterfaces())
                {
                    if (ifc.IsGenericType && ifc.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        return ifc.GetGenericArguments()[0];
                    }
                }
            }

            return null;
        }
    }
}
