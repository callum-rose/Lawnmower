using System;
using System.Collections.Generic;
using BunnyHeaven.UI;
using Game.Haptics;
using Game.Levels;
using Lawnmower.Model;
using Lawnmower.Presenters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lawnmower.Services
{
	/// <summary>
	/// The app-wide services every scene binds to: saved <see cref="Progression"/>, saved <see cref="Settings"/>, sound
	/// effects and haptics. Created before the first scene loads (no scene needs to reference anything), and on first
	/// use otherwise. Settings changes apply straight away: graphics quality, haptics toggle, mixer volumes, movement scheme.
	/// </summary>
	public static class GameServices
	{
		private static readonly ISoundEffects Silent = new SilentSoundEffects();

		private static GameServicesConfig _config;
		private static Progression _progression;
		private static GameSettings _settings;
		private static IHaptics _haptics;
		private static GraphicsQualityApplier _graphics;
		private static GameServicesHost _host;

		public static Progression Progression
		{
			get
			{
				EnsureInitialised();
				return _progression;
			}
		}

		public static GameSettings Settings
		{
			get
			{
				EnsureInitialised();
				return _settings;
			}
		}

		/// <summary>Silent until the services' GameObject exists (play mode).</summary>
		public static ISoundEffects SoundEffects => SoundEffectsOverrideForTests ?? (_host != null ? _host.Sounds : Silent);

		/// <summary>Tests set this to hear which sounds play.</summary>
		internal static ISoundEffects SoundEffectsOverrideForTests { get; set; }

		public static IHaptics Haptics
		{
			get
			{
				EnsureInitialised();
				return _haptics;
			}
		}

		/// <summary>Sounds and haptics for one play session. The caller disposes it when the session ends.</summary>
		public static GameFeedbackPresenter CreateFeedback(LevelSimulation simulation) =>
			new GameFeedbackPresenter(simulation, new LateBoundSoundEffects(), Haptics, Settings);

		/// <summary>The UI click. Kit buttons (<see cref="ButtonView"/>) already play it.</summary>
		public static void PlayClick() => SoundEffects.Play(SoundEffect.Click);

		public static void EnsureInitialised()
		{
			if (_settings == null)
			{
				Initialise(new PlayerPrefsProgressStore(), new PlayerPrefsSettingsStore());
			}
		}

		#region Lifetime

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics() => Shutdown();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Bootstrap()
		{
			EnsureInitialised();

			GameObject host = new GameObject(nameof(GameServices));
			Object.DontDestroyOnLoad(host);
			_host = host.AddComponent<GameServicesHost>();
			_host.Init(_config);
			ApplyVolumes();

			Application.quitting -= Shutdown;
			Application.quitting += Shutdown;
		}

		/// <summary>(Re)creates the services on these stores. Graphics are only applied in play mode.</summary>
		internal static void Initialise(IProgressStore progressStore, ISettingsStore settingsStore)
		{
			Detach();

			_config = Resources.Load<GameServicesConfig>(GameServicesConfig.ResourcesPath);

			if (_config == null)
			{
				Debug.LogError($"No {nameof(GameServicesConfig)} at Resources/{GameServicesConfig.ResourcesPath}");
			}

			_progression = new Progression(GetLevelOrder(_config), progressStore);
			_settings = new GameSettings(settingsStore);
			_haptics = new UnityHaptics();
			_graphics ??= _config != null
				? new GraphicsQualityApplier(_config.LowQualityLevel, _config.HighQualityLevel, _config.LowRenderScale, _config.HighRenderScale)
				: null;

			_settings.GraphicsChanged += OnGraphicsChanged;
			_settings.HapticsChanged += OnHapticsChanged;
			_settings.MusicVolumeChanged += OnVolumeChanged;
			_settings.SoundsVolumeChanged += OnVolumeChanged;
			ButtonView.AnyClicked += OnAnyButtonClicked;

			OnHapticsChanged(_settings.Haptics);
			OnGraphicsChanged(_settings.Graphics);
			ApplyVolumes();
		}

		/// <summary>Services on in-memory stores, so tests never read or write the player's PlayerPrefs.</summary>
		internal static void InitialiseForTests(IProgressStore progressStore = null, ISettingsStore settingsStore = null) =>
			Initialise(progressStore ?? new InMemoryProgressStore(), settingsStore ?? new InMemorySettingsStore());

		/// <summary>Drops the services (the next use recreates them from PlayerPrefs) and undoes graphics changes.</summary>
		internal static void Shutdown()
		{
			Detach();
			_graphics?.Restore();
			_graphics = null;
			_progression = null;
			_settings = null;
			_haptics = null;
			_config = null;
		}

		private static void Detach()
		{
			ButtonView.AnyClicked -= OnAnyButtonClicked;

			if (_settings == null)
			{
				return;
			}

			_settings.GraphicsChanged -= OnGraphicsChanged;
			_settings.HapticsChanged -= OnHapticsChanged;
			_settings.MusicVolumeChanged -= OnVolumeChanged;
			_settings.SoundsVolumeChanged -= OnVolumeChanged;
		}

		#endregion

		#region Events

		private static void OnGraphicsChanged(GraphicsQuality quality)
		{
			if (Application.isPlaying)
			{
				_graphics?.Apply(quality);
			}
		}

		private static void OnHapticsChanged(bool isOn) => HapticFeedback.Enabled = isOn;

		private static void OnVolumeChanged(float _) => ApplyVolumes();

		private static void OnAnyButtonClicked(ButtonView _) => PlayClick();

		#endregion

		#region Methods

		/// <summary>The mixer only takes SetFloat once the audio system is up, so the host also calls this from Start.</summary>
		internal static void ApplyVolumes()
		{
			if (!Application.isPlaying || _host == null || _config == null || _config.Mixer == null || _settings == null)
			{
				return;
			}

			_config.Mixer.SetFloat(_config.MusicVolumeParameter, AudioVolume.ToDecibels(_settings.MusicVolume));
			_config.Mixer.SetFloat(_config.SoundsVolumeParameter, AudioVolume.ToDecibels(_settings.SoundsVolume));
		}

		private static IEnumerable<Guid> GetLevelOrder(GameServicesConfig config)
		{
			List<Guid> order = new List<Guid>();

			if (config == null || config.Levels == null)
			{
				return order;
			}

			foreach (LevelData level in config.Levels.Levels)
			{
				if (level != null && !order.Contains(level.Id))
				{
					order.Add(level.Id);
				}
			}

			return order;
		}

		#endregion

		/// <summary>Resolves <see cref="SoundEffects"/> on every play, so a session started before the host exists still plays.</summary>
		private sealed class LateBoundSoundEffects : ISoundEffects
		{
			public void Play(SoundEffect effect) => SoundEffects.Play(effect);
		}
	}
}
