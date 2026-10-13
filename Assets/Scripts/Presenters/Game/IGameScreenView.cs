using System;

namespace Lawnmower.Presenters
{
	public readonly struct LevelCompletedInfo
	{
		public int Moves { get; }

		/// <summary>0 when unknown.</summary>
		public int Par { get; }

		/// <summary>At or under par: "LAWN PERFECT!" with the tick badge. Never true when par is unknown.</summary>
		public bool IsPerfect => IsPerfectScore(Moves, Par);

		public bool HasNextLevel { get; }

		/// <summary>Undos this attempt.</summary>
		public int Undos { get; }

		/// <summary>Seconds of play this attempt, not counting pauses.</summary>
		public double Duration { get; }

		/// <summary>The saved best including this attempt.</summary>
		public int Best { get; }

		/// <summary>This attempt beat an earlier best: show "New best: <see cref="Best"/> moves".</summary>
		public bool IsNewBest { get; }

		public bool IsFirstCompletion { get; }

		public LevelCompletedInfo(int moves, int par, bool hasNextLevel)
			: this(new AttemptStats(moves, 0, 0), par, hasNextLevel, new CompletionResult(moves, null, moves, IsPerfectScore(moves, par), false, null))
		{
		}

		public LevelCompletedInfo(AttemptStats stats, int par, bool hasNextLevel, CompletionResult result)
		{
			Moves = stats.Moves;
			Par = par;
			HasNextLevel = hasNextLevel;
			Undos = stats.Undos;
			Duration = stats.Duration;
			Best = result.Best;
			IsNewBest = result.IsNewBest;
			IsFirstCompletion = result.IsFirstCompletion;
		}

		public static bool IsPerfectScore(int moves, int par) => par > 0 && moves <= par;
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
