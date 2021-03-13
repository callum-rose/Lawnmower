using System;
using System.Collections.Generic;
using Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(GrassMaterialDataHolder),
		menuName = SoNames.GameDir + nameof(GrassMaterialDataHolder))]
	internal class GrassMaterialDataHolder : SerializedScriptableObject
	{
		[SerializeField] private SerialisedDictionary<int, Material> grassMaterials;
		
		[FoldoutGroup("Legacy"), OdinSerialize]
		private Dictionary<int, GrassData> grassColours;
		
		[FoldoutGroup("Legacy"), SerializeField]
		private float colourChannelMaxVariation = 0.1f;

		public Material GetMaterialForHeight(int height)
		{
			return !grassMaterials.ContainsKey(height) ? null : grassMaterials[height];
		}		
		
		#region Legacy
		
		public GrassData GetDataForHeight(int height)
		{
			return grassColours[height];
		}

		public Color VaryColour(Color input)
		{
			float GetRandomChannelOffset() =>
				Random.Range(-colourChannelMaxVariation, colourChannelMaxVariation);

			return input + new Color(GetRandomChannelOffset(), GetRandomChannelOffset(), GetRandomChannelOffset());
		}

		[Serializable]
		public struct GrassData
		{
			[SerializeField, FormerlySerializedAs("@base"), FormerlySerializedAs("base")]
			private Color baseColour;

			[SerializeField, FormerlySerializedAs("tip")]
			private Color tipColour;

			[SerializeField] private Vector2 colourFadeYRange;

			public Color BaseColour => baseColour;
			public Color TipColour => tipColour;

			public Vector2 ColourFadeYRange => colourFadeYRange;
		}
		
		#endregion
	}
}