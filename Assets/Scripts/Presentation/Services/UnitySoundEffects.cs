using Lawnmower.Presenters;
using UnityEngine;

namespace Lawnmower.Services
{
	/// <summary>One-shot sound effects through the mixer's SFX group, from one persistent AudioSource.</summary>
	internal sealed class UnitySoundEffects : ISoundEffects
	{
		private readonly AudioSource _source;
		private readonly GameServicesConfig _config;

		public UnitySoundEffects(AudioSource source, GameServicesConfig config)
		{
			_source = source;
			_config = config;
		}

		public void Play(SoundEffect effect)
		{
			if (_source == null || _config == null)
			{
				return;
			}

			AudioClip clip = _config.GetClip(effect);

			if (clip != null)
			{
				_source.PlayOneShot(clip);
			}
		}
	}

	/// <summary>Before the services' GameObject exists (Edit Mode, tests).</summary>
	internal sealed class SilentSoundEffects : ISoundEffects
	{
		public void Play(SoundEffect effect) { }
	}
}
