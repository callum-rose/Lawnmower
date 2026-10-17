using System.Collections.Generic;
using Game.Core;

namespace Lawnmower.Model
{
	public enum LevelStatus
	{
		Playing,
		Completed,
		Failed
	}

	public enum MoveOutcome
	{
		/// <summary>The mower moved. This was one undo step.</summary>
		Moved,
		/// <summary>The target tile can't be entered. Tiles are told, but nothing changes and nothing is recorded.</summary>
		Bumped,
		/// <summary>The target is off the level. Nothing happens.</summary>
		OutOfBounds,
		/// <summary>The level is completed and accepts no more moves.</summary>
		Rejected,
		/// <summary>
		/// The mower went down a rabbit hole whose exit is blocked and came back to where it started. Like a bump,
		/// nothing changes and nothing is recorded; <see cref="MoveResult.AttemptedSegments"/> is the path it tried.
		/// </summary>
		Bounced
	}

	/// <summary>One leg of a move: a step from input, or a jump or burrow from a tile redirect.</summary>
	public readonly struct PathSegment
	{
		private readonly GridVector? _heading;

		public GridVector From { get; }
		public GridVector To { get; }
		public MovementKind Kind { get; }

		/// <summary>To - From, except a burrow, which is the unit direction the mower was heading in.</summary>
		public GridVector Direction => _heading ?? To - From;

		public PathSegment(GridVector from, GridVector to, MovementKind kind, GridVector? heading = null)
		{
			From = from;
			To = to;
			Kind = kind;
			_heading = heading;
		}

		/// <summary>Burrow only: the hole the mower comes out of, one tile back from where it lands.</summary>
		public GridVector ExitHole => To - Direction;

		public override string ToString() => $"{Kind} {From}->{To}";
	}

	public sealed class MoveResult
	{
		private static readonly PathSegment[] NoSegments = new PathSegment[0];

		/// <summary>The input direction.</summary>
		public GridVector Direction { get; }
		public MoveOutcome Outcome { get; }

		/// <summary>Where the mower was before the move.</summary>
		public GridVector From { get; }

		/// <summary>Where the mower is after the move (equal to <see cref="From"/> unless it moved).</summary>
		public GridVector To { get; }

		/// <summary>Step P->S, then Jump S->L, and so on. Empty unless the mower moved.</summary>
		public IReadOnlyList<PathSegment> Segments { get; }

		/// <summary>Every cell visited, [From, ..., To].</summary>
		public IReadOnlyList<GridVector> Path { get; }

		/// <summary>Bounced only: the path the mower tried, ending with the burrow whose exit was blocked.</summary>
		public IReadOnlyList<PathSegment> AttemptedSegments { get; }

		/// <summary>The cell the mower bumped into, if any (directly, or at the end of a redirect).</summary>
		public GridVector? BumpedAt { get; }

		public LevelStatus StatusBefore { get; }
		public LevelStatus StatusAfter { get; }

		public bool ChangedState => Outcome == MoveOutcome.Moved;

		internal MoveResult(GridVector direction, MoveOutcome outcome, GridVector from,
			IReadOnlyList<PathSegment> segments, GridVector? bumpedAt, LevelStatus statusBefore, LevelStatus statusAfter,
			IReadOnlyList<PathSegment> attemptedSegments = null)
		{
			Direction = direction;
			Outcome = outcome;
			From = from;
			Segments = segments ?? NoSegments;
			AttemptedSegments = attemptedSegments ?? NoSegments;
			BumpedAt = bumpedAt;
			StatusBefore = statusBefore;
			StatusAfter = statusAfter;

			List<GridVector> path = new List<GridVector> { from };
			foreach (PathSegment segment in Segments)
			{
				path.Add(segment.To);
			}

			Path = path;
			To = path[path.Count - 1];
		}

		public override string ToString() => $"{Outcome} {Direction}: {string.Join(" ", Path)} ({StatusBefore}->{StatusAfter})";
	}
}
