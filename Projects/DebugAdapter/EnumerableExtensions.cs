using System;
using System.Collections.Generic;
using System.Linq;

namespace DebugAdapter
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Subrange<T>(this IEnumerable<T> values, int? start, int? count)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (start is int s)
                values = values.Skip(s);
            if (count is int c)
                values = values.Take(c);
            return values;
        }
    }
}
