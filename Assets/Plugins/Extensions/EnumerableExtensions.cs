using System.Collections.Generic;
using System.Linq;

namespace BalsamicBits.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return !(enumerable?.Any() ?? false);
        }
    }
}
