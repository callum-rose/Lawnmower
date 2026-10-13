using BunnyHeaven.UI;
using Lawnmower.Presenters;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens
{
	/// <summary>
	/// The Pause card: the level, RESUME, then Restart / Settings / Levels / Menu. Tapping outside the card resumes.
	/// Passive.
	/// </summary>
	internal class PauseCardView : MonoBehaviour
	{
		[SerializeField] private ModalCardView modal;
		[SerializeField] private ButtonView resumeButton;
		[SerializeField] private ButtonView restartButton;
		[SerializeField] private ButtonView settingsButton;
		[SerializeField] private ButtonView levelsButton;
		[SerializeField] private ButtonView menuButton;

		[Tooltip("The scrim around the card: tapping it dismisses the card.")]
		[SerializeField] private Button outsideButton;

		public ButtonView ResumeButton => resumeButton;
		public ButtonView RestartButton => restartButton;
		public ButtonView SettingsButton => settingsButton;
		public ButtonView LevelsButton => levelsButton;
		public ButtonView MenuButton => menuButton;
		public Button OutsideButton => outsideButton;

		public bool IsOpen => modal.IsOpen;
		public string Subtitle => modal.Message.text;

		public void Show(PauseInfo info)
		{
			modal.SetMessage(info.LevelNumber > 0 ? $"Level {info.LevelNumber}" : null);
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
