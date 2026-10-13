using BunnyHeaven.UI;
using Lawnmower.Presenters;
using TMPro;
using UnityEngine;

namespace Game.UI.Screens
{
	/// <summary>The Pause card (Figma 12:531): RESUME, then Restart / Settings / Levels / Menu. Passive.</summary>
	internal class PauseCardView : MonoBehaviour
	{
		[SerializeField] private ModalCardView modal;
		[SerializeField] private TMP_Text subtitle;
		[SerializeField] private ButtonView resumeButton;
		[SerializeField] private ButtonView restartButton;
		[SerializeField] private ButtonView settingsButton;
		[SerializeField] private ButtonView levelsButton;
		[SerializeField] private ButtonView menuButton;

		public ButtonView ResumeButton => resumeButton;
		public ButtonView RestartButton => restartButton;
		public ButtonView SettingsButton => settingsButton;
		public ButtonView LevelsButton => levelsButton;
		public ButtonView MenuButton => menuButton;

		public bool IsOpen => modal.IsOpen;
		public string Subtitle => subtitle.text;

		public void Show(PauseInfo info)
		{
			subtitle.text = info.LevelNumber > 0
				? $"Level {info.LevelNumber} · {info.Perfects} of {info.LevelCount} perfect"
				: $"{info.Perfects} of {info.LevelCount} perfect";
			modal.Show();
		}

		public void Hide()
		{
			if (modal.IsOpen)
			{
				modal.Hide();
			}
		}

		public void HideImmediate()
		{
			modal.gameObject.SetActive(false);
		}
	}
}
