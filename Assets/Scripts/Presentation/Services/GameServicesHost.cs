using Lawnmower.Presenters;
using UnityEngine;

namespace Lawnmower.Services
{
	/// <summary>The services' persistent GameObject: owns the SFX AudioSource.</summary>
	internal sealed class GameServicesHost : MonoBehaviour
	{
		public ISoundEffects Sounds { get; private set; } = new SilentSoundEffects();

		public void Init(GameServicesConfig config)
		{
			AudioSource source = gameObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.spatialBlend = 0;
			source.outputAudioMixerGroup = config != null ? config.SoundsGroup : null;
			Sounds = new UnitySoundEffects(source, config);
		}

		private void Start() => GameServices.ApplyVolumes();
	}
}
