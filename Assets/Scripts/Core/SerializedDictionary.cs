using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
	[Serializable]
	public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[SerializeField] private List<TKey> keys = new List<TKey>();
		[SerializeField] private List<TValue> values = new List<TValue>();

		public SerializedDictionary() : base()
		{
			
		}
		
		public SerializedDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
		{
			
		}
		
		public void OnBeforeSerialize()
		{
			keys.Clear();
			values.Clear();

			foreach (var kvp in this)
			{
				keys.Add(kvp.Key);
				values.Add(kvp.Value);
			}
		}
		
		public void OnAfterDeserialize()
		{
			Clear();
			
			for (int i = 0; i < keys.Count; i++)
				Add(keys[i], values[i]);

			keys.Clear();
			values.Clear();
		}
	}
}