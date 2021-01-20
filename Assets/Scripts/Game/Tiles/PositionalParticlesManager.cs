using Core;
using Core.EventChannels;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(PositionalParticlesManager), menuName = SONames.GameDir + nameof(PositionalParticlesManager))]
	internal sealed class PositionalParticlesManager : BaseParticlesManager, IInitialisableScriptableObject
	{
		[SerializeField, AssetsOnly] private Vector3EventChannel particlesEventChannel;

		private void OnEnable()
		{
			particlesEventChannel.EventRaised += Play;
		}

		private void OnDisable()
		{
			particlesEventChannel.EventRaised -= Play;
		}

		public void Reset()
		{
			OnDisable();
		}

		private void Play(Vector3 position)
		{
			base.Play(position, null);
		}
	}
}