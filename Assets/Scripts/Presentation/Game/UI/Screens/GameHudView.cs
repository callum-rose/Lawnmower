using BunnyHeaven.UI;
using UnityEngine;

namespace Game.UI.Screens
{
	/// <summary>
	/// The in-game HUD: Pause in the top-right corner; Undo bottom-right with Restart above it, and Redo bottom-left. Redo
	/// only shows while there's something to redo; otherwise it slides back to hide behind Undo. Passive.
	/// </summary>
	internal class GameHudView : MonoBehaviour
	{
		[SerializeField] private ButtonView pauseButton;
		[SerializeField] private ButtonView restartButton;
		[SerializeField] private ButtonView undoButton;
		[SerializeField] private ButtonView redoButton;

		[Tooltip("The pause button's corner: the board sits below its bottom edge.")]
		[SerializeField] private RectTransform topBar;

		[Tooltip("The Restart/Undo/Redo controls: the board sits above their top edge.")]
		[SerializeField] private RectTransform controls;

		[SerializeField, Range(0, 1)] private float disabledAlpha = 0.45f;

		[Tooltip("Seconds for Redo to slide out from behind Undo, or back.")]
		[SerializeField, Min(0.01f)] private float redoSlideTime = 0.22f;

		private RectTransform _redoRect;
		private RectTransform _undoRect;
		private CanvasGroup _redoGroup;
		private Vector2 _redoShownPosition;
		private bool _redoShown;
		private float _redoAmount;

		public ButtonView PauseButton => pauseButton;
		public ButtonView RestartButton => restartButton;
		public ButtonView UndoButton => undoButton;
		public ButtonView RedoButton => redoButton;
		public RectTransform TopBar => topBar;
		public RectTransform Controls => controls;

		/// <summary>Whether Redo is (or is sliding) out from behind Undo.</summary>
		public bool IsRedoShown => _redoShown;

		#region Unity

		private void Awake()
		{
			_redoRect = (RectTransform)redoButton.transform;
			_undoRect = (RectTransform)undoButton.transform;
			_redoGroup = redoButton.GetComponent<CanvasGroup>();
			_redoShownPosition = _redoRect.anchoredPosition;
			redoButton.Interactable = false;
			_redoGroup.blocksRaycasts = false;
			ApplyRedo();
		}

		private void LateUpdate()
		{
			float goal = _redoShown ? 1 : 0;
			if (!Mathf.Approximately(_redoAmount, goal))
			{
				_redoAmount = Mathf.MoveTowards(_redoAmount, goal, Time.unscaledDeltaTime / redoSlideTime);
			}

			ApplyRedo();
		}

		#endregion

		#region API

		public void ShowHistory(bool canUndo, bool canRedo)
		{
			SetEnabled(undoButton, canUndo);
			_redoShown = canRedo;
			redoButton.Interactable = canRedo;
			_redoGroup.blocksRaycasts = canRedo;
		}

		/// <summary>While a card covers the game, only the card's buttons work.</summary>
		public void SetControlsEnabled(bool enabled)
		{
			pauseButton.Interactable = enabled;
			restartButton.Interactable = enabled;
		}

		#endregion

		#region Methods

		private void SetEnabled(ButtonView button, bool enabled)
		{
			button.Interactable = enabled;

			if (button.TryGetComponent(out CanvasGroup group))
			{
				group.alpha = enabled ? 1 : disabledAlpha;
			}
		}

		/// <summary>
		/// Places Redo between Undo (hidden) and its own spot (shown). Undo is drawn over it; Redo fades out at the very end
		/// so it doesn't show through a dimmed Undo.
		/// </summary>
		private void ApplyRedo()
		{
			// Undo's position in Redo's anchored space (both pivot on their centres).
			Vector2 hidden = _redoRect.anchoredPosition + (Vector2)(_undoRect.localPosition - _redoRect.localPosition);
			float eased = Mathf.SmoothStep(0, 1, _redoAmount);
			_redoRect.anchoredPosition = Vector2.LerpUnclamped(hidden, _redoShownPosition, eased);
			_redoGroup.alpha = Mathf.Clamp01(_redoAmount * 5);
		}

		#endregion
	}
}
