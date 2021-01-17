using System;
using Game.Core;
using Game.UndoSystem;
using Newtonsoft.Json;
using UnityEngine.Assertions;

namespace Game.Tiles
{
	[Serializable]
	public abstract class Tile
	{
		[JsonIgnore] public abstract bool IsComplete { get; }

		[JsonIgnore] public virtual bool IsRuined => false;

		public abstract bool IsTraversable(bool editMode);

		public delegate void TraverseEvent(GridVector direction, Xor isInverted);

		public event TraverseEvent TraversedOnto;
		public event TraverseEvent TraversedAway;
		public event TraverseEvent BumpedInto;

		public Tile Clone()
		{
			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
			string serializeObject = JsonConvert.SerializeObject(this, jsonSerializerSettings);
			object deserializeObject = JsonConvert.DeserializeObject(serializeObject, jsonSerializerSettings);
			return deserializeObject as Tile;
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

		public override string ToString()
		{
			return GetType().ToString();
		}
	}
}