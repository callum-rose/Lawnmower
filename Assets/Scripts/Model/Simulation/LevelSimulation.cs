using System;
using Game.Core;
using Game.Tiles;
using Game.UndoSystem;

namespace Lawnmower.Model
{
	/// <summary>
	/// The one place the play rules live. Each <see cref="Move"/> plans the whole path (including
	/// tile redirects such as springs), then applies it as a single undo step and evaluates the
	/// level status exactly once.
	/// </summary>
	public sealed class LevelSimulation
	{
		public Level Level { get; }
		public IUndoSystem UndoSystem { get; }

		/// <summary>Starts at the level's start position. Raises one change per path segment.</summary>
		public IListenableProperty<GridVector> MowerPosition => _mowerPosition;

		public LevelStatus Status { get; private set; }

		/// <summary>(from, to, inverted). Raised once per move (or undo/redo of a move) that changes status.</summary>
		public event UndoableAction<LevelStatus, LevelStatus> StatusChanged;

		/// <summary>Raised from the undo record: inverted is false on Do/Redo and true on Undo.</summary>
		public event UndoableAction<MoveResult> Moved;

		/// <summary>Raised when the mower bumps into a tile it can't enter, or bounces out of a blocked rabbit hole. Not an undo step.</summary>
		public event Action<MoveResult> Bumped;

		private readonly EventProperty<GridVector> _mowerPosition;

		public LevelSimulation(Level level, IUndoSystem undoSystem)
			: this(level, undoSystem, level?.StartPosition ?? default, null)
		{
		}

		private LevelSimulation(Level level, IUndoSystem undoSystem, GridVector position, LevelStatus? status)
		{
			Level = level ?? throw new ArgumentNullException(nameof(level));
			UndoSystem = undoSystem ?? throw new ArgumentNullException(nameof(undoSystem));

			if (!level.Contains(position))
			{
				throw new ArgumentException($"Mower position {position} is outside the level");
			}

			_mowerPosition = new EventProperty<GridVector>(position);
			Status = status ?? EvaluateStatus();
		}

		#region API

		/// <summary>One call is at most one undo step.</summary>
		public MoveResult Move(GridVector direction)
		{
			if (!direction.IsCardinalUnit)
			{
				throw new ArgumentException($"Direction must be one cardinal step, got {direction}", nameof(direction));
			}

			GridVector from = _mowerPosition.Value;

			// Only Completed rejects input: a Failed level still accepts moves (free roam; it can only recover by undo).
			if (Status == LevelStatus.Completed)
			{
				return new MoveResult(direction, MoveOutcome.Rejected, from, null, null, Status, Status);
			}

			MovePlan plan = MovePlan.Create(Level, from, direction);

			switch (plan.Outcome)
			{
				case MoveOutcome.OutOfBounds:
					return new MoveResult(direction, plan.Outcome, from, null, null, Status, Status);

				case MoveOutcome.Bumped:
				{
					MoveResult result = new MoveResult(direction, plan.Outcome, from, null, plan.BumpedAt, Status, Status);
					Level.GetTile(plan.BumpedAt.Value).BumpInto(plan.BumpDirection, false);
					Bumped?.Invoke(result);
					return result;
				}

				case MoveOutcome.Bounced:
				{
					MoveResult result = new MoveResult(direction, plan.Outcome, from, null, plan.BumpedAt, Status, Status,
						plan.Segments.ToArray());
					if (plan.BumpedAt.HasValue)
					{
						Level.GetTile(plan.BumpedAt.Value).BumpInto(plan.BumpDirection, false);
					}

					Bumped?.Invoke(result);
					return result;
				}

				default:
				{
					MoveRecord record = new MoveRecord(this, plan);
					UndoSystem.Do(record);
					return record.Result;
				}
			}
		}

		/// <summary>What <see cref="Move"/> would do, without changing anything or raising events.</summary>
		public MoveResult Preview(GridVector direction)
		{
			return Clone(new Game.UndoSystem.UndoSystem()).Move(direction);
		}

		/// <summary>An immutable, comparable copy of the position, status and tile state.</summary>
		public SimulationSnapshot Snapshot()
		{
			return new SimulationSnapshot(_mowerPosition.Value, Status, Level);
		}

		/// <summary>
		/// A deep copy with the same mower position, status and tile state, an empty history and
		/// no event subscribers.
		/// </summary>
		public LevelSimulation Clone(IUndoSystem undoSystem)
		{
			return new LevelSimulation(Level.Clone(), undoSystem, _mowerPosition.Value, Status);
		}

		#endregion

		#region Internal (used by MoveRecord)

		internal void SetMowerPosition(GridVector position, Xor inverted)
		{
			_mowerPosition.SetValue(position, inverted);
		}

		internal void SetStatus(LevelStatus from, LevelStatus to, Xor inverted)
		{
			Status = to;
			StatusChanged?.Invoke(from, to, inverted);
		}

		internal void RaiseMoved(MoveResult result, Xor inverted)
		{
			Moved?.Invoke(result, inverted);
		}

		/// <summary>Ruined beats complete.</summary>
		internal LevelStatus EvaluateStatus()
		{
			if (Level.IsRuined)
			{
				return LevelStatus.Failed;
			}

			return Level.IsComplete ? LevelStatus.Completed : LevelStatus.Playing;
		}

		#endregion
	}
}
