using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[RequireComponent(typeof(ParticleSystem))]
	internal class GrassParticlesShaper : MonoBehaviour
	{
#if UNITY_EDITOR
		[ShowInInspector, Range(1, GrassTile.MaxGrassHeight)]
		private int _debugGrassHeight;
#endif

		private ParticleSystem _particles;

		private void Awake()
		{
			_particles = GetComponent<ParticleSystem>();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (_particles == null)
			{
				_particles = GetComponent<ParticleSystem>();
			}

			Set(_debugGrassHeight);
		}
#endif

		public void Set(int grassHeight)
		{
			ParticleSystem.ShapeModule shapeModule = _particles.shape;
			float t = Mathf.InverseLerp(1, 3, grassHeight);
			float height = Mathf.Lerp(0, 0.3f, t);
			shapeModule.position = Vector3.up * height;
		}
	}
}