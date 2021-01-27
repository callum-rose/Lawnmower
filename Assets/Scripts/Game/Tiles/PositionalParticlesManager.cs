using Core;
using Core.EventChannels;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(PositionalParticlesManager), menuName = SONames.GameDir + nameof(PositionalParticlesManager))]
	internal sealed class PositionalParticlesManager : BaseParticlesManager, IUnreferencedScriptableObject
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

		private void Play(Vector3 position)
		{
			base.Play(position, null);
		}
	}
}