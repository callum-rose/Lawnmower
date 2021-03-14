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

		public static TValue GetThenRemove<TValue, TKey>(this Dictionary<TKey, TValue> dictionary, TKey key)
		{
			TValue result = dictionary[key];
			dictionary.Remove(key);
			return result;
		}
		
		public static bool TryGetThenRemove<TValue, TKey>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
		{
			if (!dictionary.TryGetValue(key, out value))
			{
				return false;
			}
			
			dictionary.Remove(key);
			return true;
		}
	}
}