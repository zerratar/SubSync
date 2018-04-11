using System;
using System.Collections.Generic;

namespace SubSync.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> body)
        {
            foreach (var item in enumerable)
            {
                body(item);
            }
        }
    }
}
