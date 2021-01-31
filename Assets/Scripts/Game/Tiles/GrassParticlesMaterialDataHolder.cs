using System.Linq;
using Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(GrassParticlesMaterialDataHolder),
		menuName = SONames.GameDir + nameof(GrassParticlesMaterialDataHolder))]
	internal class GrassParticlesMaterialDataHolder : SerializedScriptableObject
	{
		[SerializeField] private SerialisedDictionary<int, Material> grassMaterials =
			new SerialisedDictionary<int, Material>(
				Enumerable.Range(1, 3).ToDictionary(i => i, i => null as Material)
			);
		
		public Material GetMaterialForHeight(int height)
		{
			return !grassMaterials.ContainsKey(height) ? null : grassMaterials[height];
		}	
	}
}