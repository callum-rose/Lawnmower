using System.Collections.Generic;
using Game.Levels;
using Lawnmower.Presenters;
using UnityEngine;
using UnityEngine.Audio;

namespace Lawnmower.Services
{
	/// <summary>The assets <see cref="GameServices"/> needs, loaded from Resources so no scene has to reference them.</summary>
	[CreateAssetMenu(fileName = nameof(GameServicesConfig), menuName = "Bunny Heaven/" + nameof(GameServicesConfig))]
	internal sealed class GameServicesConfig : ScriptableObject
	{
		public const string ResourcesPath = "BunnyHeaven/GameServicesConfig";

		[Header("Levels")]
		[SerializeField] private LevelDataManager levels;

		[Header("Audio")]
		[SerializeField] private AudioMixer mixer;
		[SerializeField] private AudioMixerGroup soundsGroup;
		[SerializeField] private string musicVolumeParameter = "MusicVolume";
		[SerializeField] private string soundsVolumeParameter = "SfxVolume";
		[SerializeField] private AudioClip move;
		[SerializeField] private AudioClip bump;
		[SerializeField] private AudioClip click;
		[SerializeField] private AudioClip win;
		[SerializeField] private AudioClip fail;

		[Header("Graphics")]
		[SerializeField] private string lowQualityLevel = "Low";
		[SerializeField] private string highQualityLevel = "High";
		[SerializeField, Range(0.25f, 1f)] private float lowRenderScale = 0.75f;
		[SerializeField, Range(0.25f, 1f)] private float highRenderScale = 1f;

		public LevelDataManager Levels => levels;
		public AudioMixer Mixer => mixer;
		public AudioMixerGroup SoundsGroup => soundsGroup;
		public string MusicVolumeParameter => musicVolumeParameter;
		public string SoundsVolumeParameter => soundsVolumeParameter;
		public string LowQualityLevel => lowQualityLevel;
		public string HighQualityLevel => highQualityLevel;
		public float LowRenderScale => lowRenderScale;
		public float HighRenderScale => highRenderScale;

		public AudioClip GetClip(SoundEffect effect) => effect switch
		{
			SoundEffect.Move => move,
			SoundEffect.Bump => bump,
			SoundEffect.Click => click,
			SoundEffect.Win => win,
			SoundEffect.Fail => fail,
			_ => null
		};

		public IEnumerable<AudioClip> AllClips => new[] { move, bump, click, win, fail };
	}
}
