using System;
using System.Collections.Generic;
using Game.Core;
using Game.UndoSystem;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Tiles
{
	[Serializable]
	internal abstract class Tile
	{
		// [SerializeField] private string data;

		[JsonIgnore]
		public abstract bool IsComplete { get; }
		
		[JsonIgnore]
		public virtual bool IsRuined => false;

		public abstract bool IsTraversable(bool editMode);

		public delegate void TraverseEvent(GridVector direction, Xor isInverted);

		public event TraverseEvent TraversedOnto;
		public event TraverseEvent TraversedAway;
		public event TraverseEvent BumpedInto;

		private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
		{
			TypeNameHandling = TypeNameHandling.All,
			NullValueHandling = NullValueHandling.Ignore
		};

		public virtual void Setup(object data)
		{
			Assert.IsNull(data);
		}

		public virtual void TraverseOnto(GridVector fromDirection, Xor inversion)
		{
			TraversedOnto?.Invoke(fromDirection, inversion);
		}

		public virtual void TraverseAway(GridVector toDirection, Xor inversion)
		{
			TraversedAway?.Invoke(toDirection, inversion);
		}

		public virtual void BumpInto(GridVector fromDirection, Xor inversion)
		{
			BumpedInto?.Invoke(fromDirection, inversion);
		}

		// public void OnBeforeSerialize()
		// {
		// 	// List<KeyValuePair<string, object>> keyValuePairs = OnSerialise();
		// 	// data = JsonConvert.SerializeObject(keyValuePairs, _serializerSettings);
		// }
		//
		// public void OnAfterDeserialize()
		// {
		// 	// List<KeyValuePair<string, object>> keyValuePairs =
		// 	// 	JsonConvert.DeserializeObject<List<KeyValuePair<string, object>>>(data);
		// 	// OnDeserialise(keyValuePairs);
		// }

		// protected virtual List<KeyValuePair<string, object>> OnSerialise()
		// {
		// 	return null;
		// }
		//
		// protected virtual void OnDeserialise(List<KeyValuePair<string, object>> keyValuePairs)
		// {
		// }

		public override string ToString()
		{
			return GetType().ToString();
		}
	}
}