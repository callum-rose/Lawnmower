using System.Collections.Generic;
using Game.Core;
using Game.Tiles;
using Game.UndoSystem;

namespace Lawnmower.Model
{
	/// <summary>
	/// The whole of one move (every path segment, any end-of-path bump and the status change) as a
	/// single undo step. Undo replays the recorded segments backwards and never consults tile
	/// redirects, so springs can't re-launch on undo (B1) or push extra steps (B2).
	/// </summary>
	internal sealed class MoveRecord : IUndoable
	{
		private readonly LevelSimulation _simulation;
		private readonly MovePlan _plan;
		private readonly LevelStatus _statusBefore;
		private LevelStatus _statusAfter;

		public MoveResult Result { get; private set; }

		public MoveRecord(LevelSimulation simulation, MovePlan plan)
		{
			_simulation = simulation;
			_plan = plan;
			_statusBefore = simulation.Status;
		}

		public void Do()
		{
			Level level = _simulation.Level;
			IReadOnlyList<PathSegment> segments = _plan.Segments;

			// Per segment: position first, then tiles (the order views were built against).
			foreach (PathSegment segment in segments)
			{
				_simulation.SetMowerPosition(segment.To, false);
				level.GetTile(segment.From).TraverseAway(segment.Direction, false);
				level.GetTile(segment.To).TraverseOnto(segment.Direction, false);
			}

			if (_plan.BumpedAt.HasValue)
			{
				level.GetTile(_plan.BumpedAt.Value).BumpInto(_plan.BumpDirection, false);
			}

			_statusAfter = _simulation.EvaluateStatus();
			Result ??= new MoveResult(_plan.Direction, MoveOutcome.Moved, _plan.From, segments, _plan.BumpedAt,
				_statusBefore, _statusAfter);

			if (_statusAfter != _statusBefore)
			{
				_simulation.SetStatus(_statusBefore, _statusAfter, false);
			}

			_simulation.RaiseMoved(Result, false);
		}

		public void Undo()
		{
			Level level = _simulation.Level;
			IReadOnlyList<PathSegment> segments = _plan.Segments;

			// Exact mirror: per segment (last first), tiles first, then position.
			for (int i = segments.Count - 1; i >= 0; i--)
			{
				PathSegment segment = segments[i];
				level.GetTile(segment.From).TraverseAway(segment.Direction, true);
				level.GetTile(segment.To).TraverseOnto(segment.Direction, true);
				_simulation.SetMowerPosition(segment.From, true);
			}

			// Restore the recorded status rather than re-evaluating it.
			if (_statusAfter != _statusBefore)
			{
				_simulation.SetStatus(_statusAfter, _statusBefore, true);
			}

			_simulation.RaiseMoved(Result, true);
		}
	}
}
