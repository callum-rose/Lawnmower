using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace BalsamicBits.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return !(enumerable?.Any() ?? false);
        }

        public static NativeArray<T> ToNativeArray<T>(this IEnumerable<T> enumerable, Allocator allocator) where T : struct
        {
            return new NativeArray<T>(enumerable.ToArray(), allocator);
        }
    }
}
