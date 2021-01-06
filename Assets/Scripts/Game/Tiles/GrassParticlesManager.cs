using Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(GrassParticlesManager), menuName = SONames.GameDir + nameof(GrassParticlesManager))]
	internal sealed class GrassParticlesManager : BaseParticlesManager
	{
		[SerializeField, AssetsOnly] private Vector3AndIntEventChannel particlesEventChannel;

		protected void OnEnable()
		{
			particlesEventChannel.EventRaised += Play;
		}

		protected void OnDisable()
		{
			particlesEventChannel.EventRaised -= Play;
		}

		private void Play(Vector3 position, int grassHeight)
		{
			void SetColour(ParticleSystem particles)
			{
				GrassParticlesColourer colourer = particles.gameObject.GetComponent<GrassParticlesColourer>();
				colourer.Set(grassHeight);
			}
			
			void SetShape(ParticleSystem particles)
			{
				GrassParticlesShaper shaper = particles.gameObject.GetComponent<GrassParticlesShaper>();
				shaper.Set(grassHeight);
			}
			
			base.Play(position, SetColour, SetShape);
		}
	}
}