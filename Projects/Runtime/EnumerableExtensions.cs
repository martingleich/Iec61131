using System;
using System.Collections.Generic;
using System.Linq;

namespace Runtime
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> IndexedValuesToEnumerable<T>(this IEnumerable<KeyValuePair<int, T>> indexedSet, T defaultValue)
        {
            int i = 0;
            foreach (var x in indexedSet.OrderBy(x => x.Key))
            {
                while (i < x.Key)
                {
                    yield return defaultValue;
                    ++i;
                }
                if (i != x.Key)
                    throw new ArgumentException();
                yield return x.Value;
                ++i;
            }
        }
    }
}
