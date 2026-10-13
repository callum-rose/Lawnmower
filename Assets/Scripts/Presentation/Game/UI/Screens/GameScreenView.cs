using System;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using UnityEngine;

namespace Game.UI.Screens
{
	/// <summary>
	/// The Game scene's screen: HUD, Pause card, Level Complete card and Level Failed sheet, on one overlay canvas.
	/// Passive: it raises the buttons' requests and shows what the presenters tell it. It also reports the screen
	/// rectangle left for the board, which <see cref="Game.Cameras.BoardCameraFramer"/> frames the board into.
	/// </summary>
	internal class GameScreenView : MonoBehaviour, IGameScreenView, IGameHudView
	{
		[SerializeField] private GameHudView hud;
		[SerializeField] private PauseCardView pauseCard;
		[SerializeField] private LevelCompleteCardView completeCard;
		[SerializeField] private LevelFailedSheetView failedSheet;
		[SerializeField] private ArrowPadView arrowPad;
		[SerializeField] private TutorialOverlayView tutorial;
		[SerializeField] private ToastView toast;

		[Tooltip("Gap kept between the board and the HUD bars, in canvas units.")]
		[SerializeField] private float boardMargin = 10;

		private readonly Vector3[] _corners = new Vector3[4];
		private int _levelNumber;

		public event Action UndoRequested;
		public event Action RedoRequested;
		public event Action RestartRequested;
		public event Action NextLevelRequested;
		public event Action LevelSelectRequested;
		public event Action PauseRequested;
		public event Action ResumeRequested;
		public event Action SettingsRequested;
		public event Action MainMenuRequested;

		public GameHudView Hud => hud;
		public PauseCardView PauseCard => pauseCard;
		public LevelCompleteCardView CompleteCard => completeCard;
		public LevelFailedSheetView FailedSheet => failedSheet;
		public ArrowPadView ArrowPad => arrowPad;
		public TutorialOverlayView Tutorial => tutorial;

		internal bool IsCompletedShown => completeCard.IsOpen;
		internal bool IsFailedShown => failedSheet.IsOpen;
		internal bool IsPausedShown => pauseCard.IsOpen;

		#region Unity

		private void Awake()
		{
			hud.PauseButton.Clicked += () => PauseRequested?.Invoke();
			hud.RestartButton.Clicked += () => RestartRequested?.Invoke();
			hud.UndoButton.Clicked += () => UndoRequested?.Invoke();
			hud.RedoButton.Clicked += () => RedoRequested?.Invoke();

			pauseCard.ResumeButton.Clicked += () => ResumeRequested?.Invoke();
			pauseCard.RestartButton.Clicked += () => RestartRequested?.Invoke();
			pauseCard.SettingsButton.Clicked += () => SettingsRequested?.Invoke();
			pauseCard.LevelsButton.Clicked += () => LevelSelectRequested?.Invoke();
			pauseCard.MenuButton.Clicked += () => MainMenuRequested?.Invoke();

			completeCard.NextButton.Clicked += () => NextLevelRequested?.Invoke();
			completeCard.ReplayButton.Clicked += () => RestartRequested?.Invoke();
			completeCard.LevelsButton.Clicked += () => LevelSelectRequested?.Invoke();

			failedSheet.UndoButton.Clicked += PressUndoLastMove;
			failedSheet.RestartButton.Clicked += () => RestartRequested?.Invoke();
			failedSheet.LevelsButton.Clicked += () => LevelSelectRequested?.Invoke();

			pauseCard.HideImmediate();
			completeCard.HideImmediate();
			failedSheet.HideImmediate();
			tutorial.gameObject.SetActive(false);
		}

		#endregion

		#region IGameScreenView

		public void ShowProgress(int moves, int par) => hud.ShowProgress(moves, par);

		public void ShowCompleted(LevelCompletedInfo info)
		{
			completeCard.Show(info, _levelNumber);
			UpdateHudEnabled();
		}

		public void HideCompleted()
		{
			completeCard.Hide();
			UpdateHudEnabled();
		}

		public void ShowFailed()
		{
			failedSheet.Show();
			UpdateHudEnabled();
		}

		public void HideFailed()
		{
			failedSheet.Hide();
			UpdateHudEnabled();
		}

		#endregion

		#region IGameHudView

		public void ShowLevel(int number)
		{
			_levelNumber = number;
			hud.ShowLevel(number);
		}

		public void ShowHistory(bool canUndo, bool canRedo) => hud.ShowHistory(canUndo, canRedo);

		public void ShowPaused(PauseInfo info) => pauseCard.Show(info);

		public void HidePaused() => pauseCard.Hide();

		#endregion

		#region API

		public void ShowToast(string message) => toast.Show(message);

		/// <summary>The Level Failed annotation points at this world position.</summary>
		public void SetFailAnchor(Func<Vector3> worldPosition, Camera camera) => failedSheet.SetAnchor(worldPosition, camera);

		/// <summary>
		/// The screen rectangle (pixels) the board should fit in: below the top bar and above whatever is at the bottom
		/// (the Level Failed sheet, else the arrows, else the Restart/Undo/Redo row).
		/// </summary>
		public Rect GetBoardScreenRect()
		{
			float top = Edge(hud.TopBar, bottom: true);
			RectTransform below = failedSheet.IsOpen ? failedSheet.Sheet
				: arrowPad.gameObject.activeInHierarchy ? arrowPad.Rect
				: hud.Controls;
			float bottom = Edge(below, bottom: false);

			float margin = boardMargin * ScaleFactor();
			float left = Screen.safeArea.xMin + margin;
			float right = Screen.safeArea.xMax - margin;
			top -= margin;
			bottom += margin;

			return Rect.MinMaxRect(left, Mathf.Min(bottom, top - 1), right, top);
		}

		/// <summary>What the Level Failed sheet's UNDO LAST MOVE does.</summary>
		internal void PressUndoLastMove() => UndoRequested?.Invoke();

		#endregion

		#region Methods

		private void UpdateHudEnabled() => hud.SetControlsEnabled(!completeCard.IsOpen && !failedSheet.IsOpen);

		/// <summary>Screen-space y of a rect's top or bottom edge (overlay canvas: world space is pixels).</summary>
		private float Edge(RectTransform rect, bool bottom)
		{
			rect.GetWorldCorners(_corners);
			return bottom ? Mathf.Min(_corners[0].y, _corners[3].y) : Mathf.Max(_corners[1].y, _corners[2].y);
		}

		private float ScaleFactor()
		{
			Canvas canvas = GetComponentInParent<Canvas>();
			return canvas != null ? canvas.rootCanvas.scaleFactor : 1;
		}

		#endregion
	}
}
