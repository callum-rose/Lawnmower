using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.UndoSystem;
using Newtonsoft.Json;

namespace Game.Tiles
{
	[Serializable]
	internal partial class GrassTile : Tile
	{
		public const int MaxGrassHeight = 3;
		private const int PerfectGrassHeight = 1;

		public override bool IsComplete => GrassHeight.Value == PerfectGrassHeight;
		public override bool IsRuined => GrassHeight.Value == 0;

		public IListenableProperty<int> GrassHeight => InternalGrassHeight;

		private EventProperty<int> InternalGrassHeight { get; }

		public GrassTile()
		{
			InternalGrassHeight = new EventProperty<int>(i => Mathf.Clamp(i, 0, MaxGrassHeight));
		}
		
		public GrassTile(int grassHeight) : this()
		{
			InternalGrassHeight.Value = grassHeight;
		}

		#region API

		public override void Setup(object data)
		{
			if (!(data is GrassTileSetupData grassData))
			{
				throw new ArgumentException($"Argument must be of type {nameof(GrassTileSetupData)}");
			}

			InternalGrassHeight.Value = grassData.grassHeight;
		}

		public override bool IsTraversable(bool editMode) => !editMode || GrassHeight.Value < MaxGrassHeight;

		public override void TraverseOnto(GridVector fromDirection, Xor inverted)
		{
			if (!inverted)
			{
				InternalGrassHeight.Value--;
			}
			else
			{
				InternalGrassHeight.Value++;

				if (InternalGrassHeight.Value > MaxGrassHeight)
				{
					throw new InvalidOperationException("Grass height too high");
				}
			}

			base.TraverseOnto(fromDirection, inverted);
		}

		public override void TraverseAway(GridVector toDirection, Xor inverted) =>
			base.TraverseAway(toDirection, inverted);

		#endregion

		#region Methods

		// protected override List<KeyValuePair<string, object>> OnSerialise()
		// {
		// 	if (InternalGrassHeight == null)
		// 	{
		// 		return null;
		// 	}
		// 	
		// 	return new List<KeyValuePair<string, object>>
		// 	{
		// 		new KeyValuePair<string, object>(nameof(InternalGrassHeight), InternalGrassHeight.Value)
		// 	};
		// }
		//
		// protected override void OnDeserialise(List<KeyValuePair<string, object>> keyValuePairs)
		// {
		// 	if (keyValuePairs == null)
		// 	{
		// 		return;
		// 	}
		// 	
		// 	foreach (KeyValuePair<string, object> kvp in keyValuePairs)
		// 	{
		// 		switch (kvp.Key)
		// 		{
		// 			case nameof(InternalGrassHeight):
		// 				try
		// 				{
		// 					InternalGrassHeight.Value = int.Parse(kvp.Value.ToString());
		// 				}
		// 				catch (Exception e)
		// 				{
		// 					Debug.Log(kvp.Value + " " + kvp.Value.GetType());
		// 					Debug.LogException(e);
		// 				}
		// 				break;
		// 		}
		// 	}
		// }

		#endregion

		public override string ToString()
		{
			return $"{GetType()}: {InternalGrassHeight.Value}";
		}
	}
}