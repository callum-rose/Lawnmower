using Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	internal abstract class ParticlesManager : ScriptableObject
	{
		[SerializeField] private Vector3EventChannel particlesEventChannel;
		[SerializeField, AssetsOnly] private ParticleSystem particlesPrefab;
		
		private void OnEnable()
		{
			Debug.Log("poop");
			particlesEventChannel.EventRaised += Emit;
		}

		private void OnDisable()
		{
			particlesEventChannel.EventRaised -= Emit;
		}

		public void Emit(Vector3 location)
		{
			ParticleSystem newParticles = Instantiate(particlesPrefab, location, Quaternion.identity);
			newParticles.Play();
		}
	}
}