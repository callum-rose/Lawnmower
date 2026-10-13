using System;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using UnityEngine;

namespace MainMenu
{
	/// <summary>Passive view of the Main Menu screen (Figma 12:5).</summary>
	public class MainMenuView : MonoBehaviour, IMainMenuView
	{
		[SerializeField] private ButtonView playButton;
		[SerializeField] private ButtonView levelsButton;
		[SerializeField] private ButtonView settingsButton;
		[SerializeField] private ButtonView howToPlayButton;
		[SerializeField] private LongPressTrigger title;
		[SerializeField] private ToastView toast;

		public event Action PlayClicked;
		public event Action LevelsClicked;
		public event Action SettingsClicked;
		public event Action HowToPlayClicked;
		public event Action TitleLongPressed;

		public ButtonView PlayButton => playButton;
		public ButtonView LevelsButton => levelsButton;
		public ButtonView SettingsButton => settingsButton;
		public ButtonView HowToPlayButton => howToPlayButton;
		public LongPressTrigger Title => title;
		public ToastView Toast => toast;
		public string PlayLabel => playButton.Label.text;
		public string ToastMessage => toast.Message.text;

		private void Awake()
		{
			playButton.Clicked += RaisePlay;
			levelsButton.Clicked += RaiseLevels;
			settingsButton.Clicked += RaiseSettings;
			howToPlayButton.Clicked += RaiseHowToPlay;
			title.LongPressed += RaiseTitleLongPressed;
		}

		private void OnDestroy()
		{
			playButton.Clicked -= RaisePlay;
			levelsButton.Clicked -= RaiseLevels;
			settingsButton.Clicked -= RaiseSettings;
			howToPlayButton.Clicked -= RaiseHowToPlay;
			title.LongPressed -= RaiseTitleLongPressed;
		}

		public void ShowPlayLevel(int? levelNumber) =>
			playButton.SetLabel(levelNumber.HasValue ? $"PLAY · LEVEL {levelNumber.Value}" : "PLAY");

		public void ShowToast(string message) => toast.Show(message);

		private void RaisePlay() => PlayClicked?.Invoke();
		private void RaiseLevels() => LevelsClicked?.Invoke();
		private void RaiseSettings() => SettingsClicked?.Invoke();
		private void RaiseHowToPlay() => HowToPlayClicked?.Invoke();
		private void RaiseTitleLongPressed() => TitleLongPressed?.Invoke();
	}
}
