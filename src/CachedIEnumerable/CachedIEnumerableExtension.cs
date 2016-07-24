using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CachedIEnumerable
{
    public static class CachedIEnumerableExtension
    {
        public static IEnumerable<T> Cache<T>(this IEnumerable<T> enumerable)
        {
            return new CachedIEnumerable<T>(enumerable);
        }
    }
}
