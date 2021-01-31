using UnityEngine;

namespace Game.Tiles
{
	[RequireComponent(typeof(ParticleSystemRenderer))]
	internal class GrassParticlesColourer : MonoBehaviour
	{
		[SerializeField] private GrassParticlesMaterialDataHolder grassMaterialDataHolder;

		[SerializeField, Range(1, GrassTile.MaxGrassHeight)]
		private int debugGrassheight = 3;

		private ParticleSystemRenderer _particlesRenderer;

		private void Awake()
		{
			_particlesRenderer = GetComponent<ParticleSystemRenderer>();

			Set(debugGrassheight);
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			Set(debugGrassheight);
		}
#endif

		public void Set(int grassHeight)
		{
#if UNITY_EDITOR
			if (!_particlesRenderer)
			{
				_particlesRenderer = GetComponent<ParticleSystemRenderer>();
			}
#endif

			_particlesRenderer.sharedMaterial = grassMaterialDataHolder.GetMaterialForHeight(grassHeight);
		}
	}
}