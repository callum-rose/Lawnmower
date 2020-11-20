using System.Collections.Generic;
using System.Linq;

namespace BalsamicBits.Extensions
{
    public static class DictionaryExtensions
    {
        public static Dictionary<TValue, TKey> Mirror<TValue, TKey>(this Dictionary<TKey, TValue> dictionary)
        {
            return dictionary.ToDictionary(kv => kv.Value, kv => kv.Key);
        }
    }
}
