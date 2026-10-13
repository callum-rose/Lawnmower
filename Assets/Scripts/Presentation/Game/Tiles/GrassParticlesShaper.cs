using UnityEngine;

namespace Game.Tiles
{
	[RequireComponent(typeof(ParticleSystem))]
	internal class GrassParticlesShaper : MonoBehaviour
	{
		private ParticleSystem _particles;

		private void Awake()
		{
			_particles = GetComponent<ParticleSystem>();
		}

		public void Set(int grassHeight)
		{
			ParticleSystem.ShapeModule shapeModule = _particles.shape;
			float t = Mathf.InverseLerp(1, 3, grassHeight);
			float height = Mathf.Lerp(0, 0.3f, t);
			shapeModule.position = Vector3.up * height;
		}
	}
}