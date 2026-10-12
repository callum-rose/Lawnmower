using System.Linq;
using Core;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(GrassParticlesMaterialDataHolder),
		menuName = SoNames.GameDir + nameof(GrassParticlesMaterialDataHolder))]
	internal class GrassParticlesMaterialDataHolder : ScriptableObject
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