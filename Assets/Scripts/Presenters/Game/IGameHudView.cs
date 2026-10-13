using System;

namespace Lawnmower.Presenters
{
	/// <summary>What the Pause card shows: the current level.</summary>
	public readonly struct PauseInfo
	{
		/// <summary>1-based; 0 when the level isn't in the level list.</summary>
		public int LevelNumber { get; }

		public PauseInfo(int levelNumber)
		{
			LevelNumber = levelNumber;
		}
	}

	/// <summary>The in-game HUD around the board (undo/redo) and the Pause card.</summary>
	public interface IGameHudView
	{
		/// <summary>1-based; 0 when the level isn't in the level list.</summary>
		void ShowLevel(int number);

		void ShowHistory(bool canUndo, bool canRedo);

		void ShowPaused(PauseInfo info);
		void HidePaused();

		event Action PauseRequested;
		event Action ResumeRequested;
		event Action SettingsRequested;
		event Action MainMenuRequested;
	}

	/// <summary>Leaving the game for the Main Menu (the Pause card's MENU button).</summary>
	public interface IGameMenuNavigator
	{
		void LoadMainMenu();
	}
}
