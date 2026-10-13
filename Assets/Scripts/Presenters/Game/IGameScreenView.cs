using System;

namespace Lawnmower.Presenters
{
	public readonly struct LevelCompletedInfo
	{
		public int Moves { get; }

		/// <summary>0 when unknown.</summary>
		public int Par { get; }

		public bool IsPerfect => Par > 0 && Moves <= Par;
		public bool HasNextLevel { get; }

		public LevelCompletedInfo(int moves, int par, bool hasNextLevel)
		{
			Moves = moves;
			Par = par;
			HasNextLevel = hasNextLevel;
		}
	}

	/// <summary>The game screen around the board: HUD, the completed card and the failed card.</summary>
	public interface IGameScreenView
	{
		/// <summary>Moves so far this attempt, and the level's par (0 when unknown).</summary>
		void ShowProgress(int moves, int par);

		void ShowCompleted(LevelCompletedInfo info);
		void HideCompleted();

		void ShowFailed();
		void HideFailed();

		event Action UndoRequested;
		event Action RedoRequested;
		event Action RestartRequested;
		event Action NextLevelRequested;
		event Action LevelSelectRequested;
	}
}
