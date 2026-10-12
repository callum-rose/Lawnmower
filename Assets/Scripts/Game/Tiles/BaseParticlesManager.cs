using System;
using System.Collections;
using Core;
using UnityEngine;

namespace Game.Tiles
{
	internal abstract class BaseParticlesManager : ScriptableObjectWithCoroutines
	{		
		[SerializeField] private ParticleSystem particlesPrefab;
		
		// ReSharper disable Unity.PerformanceAnalysis
		protected void Play(Vector3 position, params Action<ParticleSystem>[] middlewares)
		{
			ParticleSystem newParticles = CreateParticlesInstance(position);
			
			if (middlewares != null)
			{
				foreach (Action<ParticleSystem> action in middlewares)
				{
					action?.Invoke(newParticles);
				}
			}

			StartCoroutine(PlayThenDestroyWhenFinishedRoutine(newParticles));
		}

		private ParticleSystem CreateParticlesInstance(Vector3 position)
		{
			return Instantiate(particlesPrefab, position, Quaternion.identity);
		}

		private IEnumerator PlayThenDestroyWhenFinishedRoutine(ParticleSystem particles)
		{
			particles.Play();
			
			yield return new WaitWhile(particles.IsAlive);
			
			Destroy(particles.gameObject);
		}
	}
}