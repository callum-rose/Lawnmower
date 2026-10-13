using Core;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(GrassMaterialDataHolder),
		menuName = SoNames.GameDir + nameof(GrassMaterialDataHolder))]
	internal class GrassMaterialDataHolder : ScriptableObject
	{
		[SerializeField] private SerialisedDictionary<int, Material> grassMaterials;

		public Material GetMaterialForHeight(int height)
		{
			return !grassMaterials.ContainsKey(height) ? null : grassMaterials[height];
		}
	}
}
