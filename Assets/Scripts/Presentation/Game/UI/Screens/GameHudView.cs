using BunnyHeaven.UI;
using UnityEngine;

namespace Game.UI.Screens
{
	/// <summary>The in-game HUD (Figma 12:330): pause, level and moves/par pills, Restart/Undo/Redo. Passive.</summary>
	internal class GameHudView : MonoBehaviour
	{
		[SerializeField] private ButtonView pauseButton;
		[SerializeField] private PillView levelPill;
		[SerializeField] private PillView progressPill;
		[SerializeField] private ButtonView restartButton;
		[SerializeField] private ButtonView undoButton;
		[SerializeField] private ButtonView redoButton;

		[Tooltip("The top bar: the board sits below its bottom edge.")]
		[SerializeField] private RectTransform topBar;

		[Tooltip("The Restart/Undo/Redo row: the board sits above its top edge.")]
		[SerializeField] private RectTransform controls;

		[SerializeField, Range(0, 1)] private float disabledAlpha = 0.45f;

		public ButtonView PauseButton => pauseButton;
		public ButtonView RestartButton => restartButton;
		public ButtonView UndoButton => undoButton;
		public ButtonView RedoButton => redoButton;
		public RectTransform TopBar => topBar;
		public RectTransform Controls => controls;

		public string LevelText => levelPill.Label.text;
		public string ProgressText => progressPill.Label.text;

		public void ShowLevel(int number) => levelPill.SetText(number > 0 ? $"LEVEL {number}" : "LEVEL");

		/// <summary>Moves / par, e.g. "12/20"; just the moves when par is unknown.</summary>
		public void ShowProgress(int moves, int par) => progressPill.SetText(par > 0 ? $"{moves}/{par}" : moves.ToString());

		public void ShowHistory(bool canUndo, bool canRedo)
		{
			SetEnabled(undoButton, canUndo);
			SetEnabled(redoButton, canRedo);
		}

		/// <summary>While a card covers the game, only the card's buttons work.</summary>
		public void SetControlsEnabled(bool enabled)
		{
			pauseButton.Interactable = enabled;
			restartButton.Interactable = enabled;
		}

		private void SetEnabled(ButtonView button, bool enabled)
		{
			button.Interactable = enabled;

			if (button.TryGetComponent(out CanvasGroup group))
			{
				group.alpha = enabled ? 1 : disabledAlpha;
			}
		}
	}
}
