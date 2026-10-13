using System;

namespace Lawnmower.Presenters
{
	public interface ISettingsView
	{
		void ShowGraphics(GraphicsQuality quality);
		void ShowMovement(MovementScheme scheme);
		void ShowHaptics(bool isOn);
		void ShowMusicVolume(float volume);
		void ShowSoundsVolume(float volume);
		void ShowVersion(string version);

		/// <summary>The "Reset progress?" modal. Nothing is wiped until the player confirms it.</summary>
		void ShowResetConfirmation(bool isOpen);

		/// <summary>A short confirmation, e.g. "Progress reset".</summary>
		void ShowMessage(string message);

		event Action<GraphicsQuality> GraphicsChosen;
		event Action<MovementScheme> MovementChosen;
		event Action<bool> HapticsChosen;
		event Action<float> MusicVolumeChosen;
		event Action<float> SoundsVolumeChosen;
		event Action ReplayTutorialRequested;
		event Action ResetProgressRequested;
		event Action ResetConfirmed;
		event Action ResetCancelled;
		event Action BackRequested;
	}

	public interface ISettingsNavigator
	{
		/// <summary>Closes the Settings overlay, back to whatever opened it (Main Menu or Pause).</summary>
		void Close();

		/// <summary>Closes Settings and starts the how-to-play tutorial.</summary>
		void ReplayTutorial();
	}

	/// <summary>
	/// Settings screen: every control writes straight to <see cref="GameSettings"/> (which saves and applies it), and
	/// the view follows the settings' change events. Reset Progress only wipes <see cref="Progression"/> after the
	/// confirmation modal's yes.
	/// </summary>
	public sealed class SettingsPresenter : IDisposable
	{
		public const string ProgressResetMessage = "Progress reset";

		private readonly ISettingsView _view;
		private readonly GameSettings _settings;
		private readonly Progression _progression;
		private readonly ISettingsNavigator _navigator;
		private readonly string _version;

		private bool _isStarted;

		public bool IsConfirmingReset { get; private set; }

		public SettingsPresenter(ISettingsView view, GameSettings settings, Progression progression, ISettingsNavigator navigator,
			string version)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			_progression = progression ?? throw new ArgumentNullException(nameof(progression));
			_navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
			_version = version ?? string.Empty;
		}

		public void Start()
		{
			if (_isStarted)
			{
				return;
			}

			_isStarted = true;

			_view.GraphicsChosen += OnGraphicsChosen;
			_view.MovementChosen += OnMovementChosen;
			_view.HapticsChosen += OnHapticsChosen;
			_view.MusicVolumeChosen += OnMusicVolumeChosen;
			_view.SoundsVolumeChosen += OnSoundsVolumeChosen;
			_view.ReplayTutorialRequested += OnReplayTutorialRequested;
			_view.ResetProgressRequested += OnResetProgressRequested;
			_view.ResetConfirmed += OnResetConfirmed;
			_view.ResetCancelled += OnResetCancelled;
			_view.BackRequested += OnBackRequested;

			_settings.GraphicsChanged += _view.ShowGraphics;
			_settings.MovementChanged += _view.ShowMovement;
			_settings.HapticsChanged += _view.ShowHaptics;
			_settings.MusicVolumeChanged += _view.ShowMusicVolume;
			_settings.SoundsVolumeChanged += _view.ShowSoundsVolume;

			_view.ShowGraphics(_settings.Graphics);
			_view.ShowMovement(_settings.Movement);
			_view.ShowHaptics(_settings.Haptics);
			_view.ShowMusicVolume(_settings.MusicVolume);
			_view.ShowSoundsVolume(_settings.SoundsVolume);
			_view.ShowVersion(_version);
			_view.ShowResetConfirmation(false);
		}

		public void Dispose()
		{
			if (!_isStarted)
			{
				return;
			}

			_isStarted = false;

			_view.GraphicsChosen -= OnGraphicsChosen;
			_view.MovementChosen -= OnMovementChosen;
			_view.HapticsChosen -= OnHapticsChosen;
			_view.MusicVolumeChosen -= OnMusicVolumeChosen;
			_view.SoundsVolumeChosen -= OnSoundsVolumeChosen;
			_view.ReplayTutorialRequested -= OnReplayTutorialRequested;
			_view.ResetProgressRequested -= OnResetProgressRequested;
			_view.ResetConfirmed -= OnResetConfirmed;
			_view.ResetCancelled -= OnResetCancelled;
			_view.BackRequested -= OnBackRequested;

			_settings.GraphicsChanged -= _view.ShowGraphics;
			_settings.MovementChanged -= _view.ShowMovement;
			_settings.HapticsChanged -= _view.ShowHaptics;
			_settings.MusicVolumeChanged -= _view.ShowMusicVolume;
			_settings.SoundsVolumeChanged -= _view.ShowSoundsVolume;
		}

		private void OnGraphicsChosen(GraphicsQuality quality) => _settings.Graphics = quality;

		private void OnMovementChosen(MovementScheme scheme) => _settings.Movement = scheme;

		private void OnHapticsChosen(bool isOn) => _settings.Haptics = isOn;

		private void OnMusicVolumeChosen(float volume) => _settings.MusicVolume = volume;

		private void OnSoundsVolumeChosen(float volume) => _settings.SoundsVolume = volume;

		private void OnReplayTutorialRequested()
		{
			if (!IsConfirmingReset)
			{
				_navigator.ReplayTutorial();
			}
		}

		private void OnResetProgressRequested()
		{
			IsConfirmingReset = true;
			_view.ShowResetConfirmation(true);
		}

		private void OnResetConfirmed()
		{
			if (!IsConfirmingReset)
			{
				return;
			}

			IsConfirmingReset = false;
			_progression.ResetProgress();
			_view.ShowResetConfirmation(false);
			_view.ShowMessage(ProgressResetMessage);
		}

		private void OnResetCancelled()
		{
			if (!IsConfirmingReset)
			{
				return;
			}

			IsConfirmingReset = false;
			_view.ShowResetConfirmation(false);
		}

		/// <summary>Back closes the confirmation first if it's open (Android-style back / a stray tap), else Settings.</summary>
		private void OnBackRequested()
		{
			if (IsConfirmingReset)
			{
				OnResetCancelled();
				return;
			}

			_navigator.Close();
		}
	}
}
