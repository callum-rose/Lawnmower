using System;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using TMPro;
using UnityEngine;

namespace Lawnmower.Screens
{
	/// <summary>The Settings screen (Figma 12:770). Passive: shows what the presenter gives it and reports every control.</summary>
	public class SettingsView : MonoBehaviour, ISettingsView
	{
		[SerializeField] private ButtonView backButton;
		[SerializeField] private ToggleSegmentView graphics;
		[SerializeField] private ToggleSegmentView movement;
		[SerializeField] private ToggleSwitchView haptics;
		[SerializeField] private SliderView music;
		[SerializeField] private SliderView sounds;
		[SerializeField] private ListRowView replayTutorial;
		[SerializeField] private ListRowView resetProgress;
		[SerializeField] private ModalCardView resetModal;
		[SerializeField] private ButtonView confirmReset;
		[SerializeField] private ButtonView cancelReset;
		[SerializeField] private TMP_Text version;
		[SerializeField] private ToastView toast;

		public event Action<GraphicsQuality> GraphicsChosen;
		public event Action<MovementScheme> MovementChosen;
		public event Action<bool> HapticsChosen;
		public event Action<float> MusicVolumeChosen;
		public event Action<float> SoundsVolumeChosen;
		public event Action ReplayTutorialRequested;
		public event Action ResetProgressRequested;
		public event Action ResetConfirmed;
		public event Action ResetCancelled;
		public event Action BackRequested;

		public ButtonView BackButton => backButton;
		public ToggleSegmentView Graphics => graphics;
		public ToggleSegmentView Movement => movement;
		public ToggleSwitchView Haptics => haptics;
		public SliderView Music => music;
		public SliderView Sounds => sounds;
		public ListRowView ReplayTutorialRow => replayTutorial;
		public ListRowView ResetProgressRow => resetProgress;
		public ModalCardView ResetModal => resetModal;
		public ButtonView ConfirmReset => confirmReset;
		public ButtonView CancelReset => cancelReset;
		public ToastView Toast => toast;

		private void Awake()
		{
			backButton.Clicked += OnBack;
			graphics.SelectionChanged += OnGraphics;
			movement.SelectionChanged += OnMovement;
			haptics.ValueChanged += OnHaptics;
			music.ValueChanged += OnMusic;
			sounds.ValueChanged += OnSounds;
			replayTutorial.Clicked += OnReplayTutorial;
			resetProgress.Clicked += OnResetProgress;
			confirmReset.Clicked += OnConfirmReset;
			cancelReset.Clicked += OnCancelReset;
		}

		private void OnDestroy()
		{
			backButton.Clicked -= OnBack;
			graphics.SelectionChanged -= OnGraphics;
			movement.SelectionChanged -= OnMovement;
			haptics.ValueChanged -= OnHaptics;
			music.ValueChanged -= OnMusic;
			sounds.ValueChanged -= OnSounds;
			replayTutorial.Clicked -= OnReplayTutorial;
			resetProgress.Clicked -= OnResetProgress;
			confirmReset.Clicked -= OnConfirmReset;
			cancelReset.Clicked -= OnCancelReset;
		}

		public void ShowGraphics(GraphicsQuality quality) =>
			graphics.SetSelectedWithoutNotify(quality == GraphicsQuality.Low ? 0 : 1);

		public void ShowMovement(MovementScheme scheme) =>
			movement.SetSelectedWithoutNotify(scheme == MovementScheme.Swipe ? 0 : 1);

		public void ShowHaptics(bool isOn) => haptics.SetIsOnWithoutNotify(isOn);

		public void ShowMusicVolume(float volume) => music.SetValueWithoutNotify(volume);

		public void ShowSoundsVolume(float volume) => sounds.SetValueWithoutNotify(volume);

		public void ShowVersion(string value)
		{
			if (version != null)
			{
				version.text = $"v{value}";
			}
		}

		public void ShowResetConfirmation(bool isOpen)
		{
			if (isOpen == resetModal.IsOpen && isOpen == resetModal.gameObject.activeSelf)
			{
				return;
			}

			if (isOpen)
			{
				resetModal.Show();
			}
			else if (resetModal.gameObject.activeSelf)
			{
				resetModal.Hide();
			}
		}

		public void ShowMessage(string message) => toast.Show(message, 1.5f);

		private void OnBack() => BackRequested?.Invoke();

		private void OnGraphics(int index) => GraphicsChosen?.Invoke(index == 0 ? GraphicsQuality.Low : GraphicsQuality.High);

		private void OnMovement(int index) => MovementChosen?.Invoke(index == 0 ? MovementScheme.Swipe : MovementScheme.Arrows);

		private void OnHaptics(bool isOn) => HapticsChosen?.Invoke(isOn);

		private void OnMusic(float value) => MusicVolumeChosen?.Invoke(value);

		private void OnSounds(float value) => SoundsVolumeChosen?.Invoke(value);

		private void OnReplayTutorial() => ReplayTutorialRequested?.Invoke();

		private void OnResetProgress() => ResetProgressRequested?.Invoke();

		private void OnConfirmReset() => ResetConfirmed?.Invoke();

		private void OnCancelReset() => ResetCancelled?.Invoke();
	}
}
