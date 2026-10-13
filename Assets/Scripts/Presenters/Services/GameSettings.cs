using System;

namespace Lawnmower.Presenters
{
	/// <summary>Settings > Graphics.</summary>
	public enum GraphicsQuality
	{
		Low,
		High
	}

	/// <summary>Everything <see cref="GameSettings"/> saves. Plain fields so a JSON serialiser can write it.</summary>
	[Serializable]
	public sealed class SettingsData
	{
		public const int CurrentVersion = 1;

		public int version = CurrentVersion;
		public GraphicsQuality graphics = GameSettings.DefaultGraphics;
		public MovementScheme movement = MovementSchemeSetting.Default;
		public bool haptics = true;
		public float musicVolume = GameSettings.DefaultVolume;
		public float soundsVolume = GameSettings.DefaultVolume;

		public SettingsData Clone() => (SettingsData)MemberwiseClone();
	}

	/// <summary>Where <see cref="GameSettings"/> keeps its data. The Unity implementation is JSON in PlayerPrefs.</summary>
	public interface ISettingsStore
	{
		/// <summary>The saved data, or null when nothing has been saved.</summary>
		SettingsData Load();

		void Save(SettingsData data);
	}

	public sealed class InMemorySettingsStore : ISettingsStore
	{
		private SettingsData _data;

		public int SaveCount { get; private set; }

		public InMemorySettingsStore(SettingsData initial = null)
		{
			_data = initial?.Clone();
		}

		public SettingsData Load() => _data?.Clone();

		public void Save(SettingsData data)
		{
			_data = data?.Clone();
			SaveCount++;
		}
	}

	/// <summary>
	/// The player's saved settings. Each setter saves straight away and raises its own event plus <see cref="Changed"/>
	/// (setting the current value does nothing). <see cref="Movement"/> also drives <see cref="MovementSchemeSetting.Current"/>.
	/// </summary>
	public sealed class GameSettings
	{
		public const GraphicsQuality DefaultGraphics = GraphicsQuality.High;
		public const float DefaultVolume = 1f;

		private readonly ISettingsStore _store;
		private readonly SettingsData _data;

		public event Action<GraphicsQuality> GraphicsChanged;
		public event Action<MovementScheme> MovementChanged;
		public event Action<bool> HapticsChanged;
		public event Action<float> MusicVolumeChanged;
		public event Action<float> SoundsVolumeChanged;

		/// <summary>Raised after any setting changes.</summary>
		public event Action Changed;

		public GameSettings(ISettingsStore store)
		{
			_store = store ?? throw new ArgumentNullException(nameof(store));
			_data = Sanitise(store.Load());
			MovementSchemeSetting.Current = _data.movement;
		}

		public GraphicsQuality Graphics
		{
			get => _data.graphics;
			set
			{
				if (!Enum.IsDefined(typeof(GraphicsQuality), value))
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
				}

				if (_data.graphics == value)
				{
					return;
				}

				_data.graphics = value;
				SaveAndNotify(GraphicsChanged, value);
			}
		}

		public MovementScheme Movement
		{
			get => _data.movement;
			set
			{
				if (!Enum.IsDefined(typeof(MovementScheme), value))
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
				}

				MovementSchemeSetting.Current = value;

				if (_data.movement == value)
				{
					return;
				}

				_data.movement = value;
				SaveAndNotify(MovementChanged, value);
			}
		}

		public bool Haptics
		{
			get => _data.haptics;
			set
			{
				if (_data.haptics == value)
				{
					return;
				}

				_data.haptics = value;
				SaveAndNotify(HapticsChanged, value);
			}
		}

		/// <summary>0 (off) to 1 (full). Values outside are clamped. There's no music yet; the slider still works.</summary>
		public float MusicVolume
		{
			get => _data.musicVolume;
			set
			{
				value = ClampVolume(value);

				if (_data.musicVolume.Equals(value))
				{
					return;
				}

				_data.musicVolume = value;
				SaveAndNotify(MusicVolumeChanged, value);
			}
		}

		/// <summary>0 (off) to 1 (full). Values outside are clamped.</summary>
		public float SoundsVolume
		{
			get => _data.soundsVolume;
			set
			{
				value = ClampVolume(value);

				if (_data.soundsVolume.Equals(value))
				{
					return;
				}

				_data.soundsVolume = value;
				SaveAndNotify(SoundsVolumeChanged, value);
			}
		}

		private void SaveAndNotify<T>(Action<T> specific, T value)
		{
			_store.Save(_data.Clone());
			specific?.Invoke(value);
			Changed?.Invoke();
		}

		private static SettingsData Sanitise(SettingsData loaded)
		{
			SettingsData data = loaded?.Clone() ?? new SettingsData();

			if (!Enum.IsDefined(typeof(GraphicsQuality), data.graphics))
			{
				data.graphics = DefaultGraphics;
			}

			if (!Enum.IsDefined(typeof(MovementScheme), data.movement))
			{
				data.movement = MovementSchemeSetting.Default;
			}

			data.musicVolume = ClampVolume(data.musicVolume);
			data.soundsVolume = ClampVolume(data.soundsVolume);
			return data;
		}

		private static float ClampVolume(float value) => float.IsNaN(value) ? DefaultVolume : Math.Max(0f, Math.Min(1f, value));
	}
}
