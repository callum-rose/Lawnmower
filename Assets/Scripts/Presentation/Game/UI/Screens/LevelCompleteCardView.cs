using System;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using TMPro;
using UnityEngine;

namespace Game.UI.Screens
{
	/// <summary>
	/// The Level Complete card (Figma 12:910). At or under par: green "LAWN / PERFECT!" banner with the tick badge;
	/// over par: blue "LAWN / DONE!" banner, no badge, "Par: N moves" instead. Passive.
	/// </summary>
	internal class LevelCompleteCardView : MonoBehaviour
	{
		[SerializeField] private ModalCardView modal;
		[SerializeField] private FrameView banner;
		[SerializeField] private TMP_Text headline;
		[SerializeField] private GameObject badge;
		[SerializeField] private TMP_Text parLine;
		[SerializeField] private TMP_Text levelLine;
		[SerializeField] private TMP_Text movesValue;
		[SerializeField] private TMP_Text undosValue;
		[SerializeField] private TMP_Text timeValue;
		[SerializeField] private TMP_Text bestLine;
		[SerializeField] private ThemedGraphic bestLineColor;
		[SerializeField] private ButtonView nextButton;
		[SerializeField] private ButtonView replayButton;
		[SerializeField] private ButtonView levelsButton;

		public ButtonView NextButton => nextButton;
		public ButtonView ReplayButton => replayButton;
		public ButtonView LevelsButton => levelsButton;

		public bool IsOpen => modal.IsOpen;
		public bool IsPerfectShown => badge.activeSelf;
		public string Headline => headline.text;
		public string BestLine => bestLine.text;

		public void Show(LevelCompletedInfo info, int levelNumber)
		{
			bool perfect = info.IsPerfect;
			banner.SetFill(perfect ? ThemeColor.Green : ThemeColor.Blue);
			headline.text = perfect ? "PERFECT!" : "DONE!";
			badge.SetActive(perfect);
			parLine.gameObject.SetActive(!perfect && info.Par > 0);
			parLine.text = $"Par: {info.Par} moves";
			levelLine.text = levelNumber > 0 ? $"LEVEL {levelNumber}" : "";

			movesValue.text = info.Moves.ToString();
			undosValue.text = info.Undos.ToString();
			timeValue.text = FormatTime(info.Duration);

			bestLine.text = info.IsNewBest ? $"New best: {info.Best} moves!" : $"Best: {info.Best} moves";
			bestLineColor.Token = info.IsNewBest ? ThemeColor.Red : ThemeColor.Muted;

			nextButton.gameObject.SetActive(info.HasNextLevel);
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

		/// <summary>m:ss</summary>
		public static string FormatTime(double seconds)
		{
			TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, Math.Floor(seconds)));
			return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
		}
	}
}
