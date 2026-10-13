using System;
using System.Collections.Generic;
using Game.Core;
using Lawnmower.Model;

namespace Lawnmower.Presenters.Tests
{
	/// <summary>Every view call, in order, as readable lines.</summary>
	public sealed class ViewLog
	{
		public List<string> Lines { get; } = new List<string>();

		public void Add(string line) => Lines.Add(line);

		public int Count(string prefix) => Lines.FindAll(l => l.StartsWith(prefix)).Count;

		public override string ToString() => string.Join("\n", Lines);
	}

	public sealed class FakeBoard : IBoardView
	{
		private readonly ViewLog _log;
		public Level Shown { get; private set; }

		public FakeBoard(ViewLog log) => _log = log;

		public void Show(Level level)
		{
			Shown = level;
			_log.Add($"board show {level.Width}x{level.Depth}");
		}

		public void Clear() => _log.Add("board clear");
	}

	public sealed class FakeMower : IMowerView
	{
		private readonly ViewLog _log;

		/// <summary>Runs inside each view call, to simulate views reacting synchronously.</summary>
		public Action<MoveResult> OnPlayMove;

		public FakeMower(ViewLog log) => _log = log;

		public void Show(GridVector start) => _log.Add($"mower show {start}");

		public void PlayMove(MoveResult move)
		{
			_log.Add($"mower play {string.Join(" ", move.Segments)}");
			OnPlayMove?.Invoke(move);
		}

		public void RewindMove(MoveResult move) => _log.Add($"mower rewind {move.To}->{move.From}");
		public void PlayBump(MoveResult bump) => _log.Add($"mower bump {bump.BumpedAt}");
		public void Hide() => _log.Add("mower hide");
	}

	public sealed class FakeScreen : IGameScreenView
	{
		private readonly ViewLog _log;

		public event Action UndoRequested;
		public event Action RedoRequested;
		public event Action RestartRequested;
		public event Action NextLevelRequested;
		public event Action LevelSelectRequested;

		public int Moves { get; private set; }
		public LevelCompletedInfo? Completed { get; private set; }
		public bool IsFailedShown { get; private set; }

		public FakeScreen(ViewLog log) => _log = log;

		public void ShowProgress(int moves, int par)
		{
			Moves = moves;
			_log.Add($"progress {moves}/{par}");
		}

		public void ShowCompleted(LevelCompletedInfo info)
		{
			Completed = info;
			_log.Add($"completed {info.Moves}/{info.Par} perfect={info.IsPerfect} next={info.HasNextLevel}");
		}

		public void HideCompleted()
		{
			Completed = null;
			_log.Add("hide completed");
		}

		public void ShowFailed()
		{
			IsFailedShown = true;
			_log.Add("failed");
		}

		public void HideFailed()
		{
			IsFailedShown = false;
			_log.Add("hide failed");
		}

		public void PressUndo() => UndoRequested?.Invoke();
		public void PressRedo() => RedoRequested?.Invoke();
		public void PressRestart() => RestartRequested?.Invoke();
		public void PressNext() => NextLevelRequested?.Invoke();
		public void PressLevelSelect() => LevelSelectRequested?.Invoke();
	}

	public sealed class FakeNavigator : IGameNavigator
	{
		private readonly ViewLog _log;
		public bool HasNextLevel { get; set; } = true;

		public FakeNavigator(ViewLog log) => _log = log;

		public void LoadNextLevel() => _log.Add("nav next");
		public void RestartLevel() => _log.Add("nav restart");
		public void LoadLevelSelect() => _log.Add("nav level select");
	}

	public sealed class FakeProgress : ILevelProgress
	{
		public List<(Guid id, AttemptStats stats, int par)> Records { get; } = new List<(Guid, AttemptStats, int)>();

		public void RecordCompleted(Guid levelId, AttemptStats stats, int par) => Records.Add((levelId, stats, par));
	}

	public sealed class FakeClock : IClock
	{
		public double Now { get; set; }
	}

	public sealed class FakeInput : IMoveInput
	{
		public event Action<GridVector> MoveRequested;

		public bool HasListeners => MoveRequested != null;

		public void Press(GridVector direction) => MoveRequested?.Invoke(direction);
	}
}
