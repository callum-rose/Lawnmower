using System;
using System.Collections.Generic;
using Game.Core;
using Game.Tiles;

namespace Lawnmower.Model
{
	/// <summary>A planned move. Planning is pure: it reads the level but never mutates it.</summary>
	internal sealed class MovePlan
	{
		public GridVector Direction { get; private set; }
		public GridVector From { get; private set; }
		public MoveOutcome Outcome { get; private set; }
		public List<PathSegment> Segments { get; } = new List<PathSegment>();
		public GridVector? BumpedAt { get; private set; }
		public GridVector BumpDirection { get; private set; }

		public static MovePlan Create(Level level, GridVector from, GridVector direction)
		{
			MovePlan plan = new MovePlan { Direction = direction, From = from };

			GridVector target = from + direction;
			if (!level.Contains(target))
			{
				plan.Outcome = MoveOutcome.OutOfBounds;
				return plan;
			}

			if (!level.GetTile(target).IsTraversable)
			{
				plan.Outcome = MoveOutcome.Bumped;
				plan.BumpedAt = target;
				plan.BumpDirection = direction;
				return plan;
			}

			plan.Outcome = MoveOutcome.Moved;
			plan.Segments.Add(new PathSegment(from, target, MovementKind.Step));

			GridVector current = target;
			GridVector arrival = direction;
			HashSet<(GridVector, GridVector)> visited = new HashSet<(GridVector, GridVector)> { (current, arrival) };

			while (true)
			{
				TileRedirect? redirect = level.GetTile(current).Redirect(current, arrival);
				if (redirect == null)
				{
					break;
				}

				GridVector next = redirect.Value.Target;
				GridVector delta = next - current;

				if (!level.Contains(next))
				{
					// Launched off the level: stay where we are, as the old spring did.
					break;
				}

				Tile nextTile = level.GetTile(next);
				if (!nextTile.IsTraversable)
				{
					plan.BumpedAt = next;
					plan.BumpDirection = delta;
					break;
				}

				GridVector nextArrival = ToUnitIfAxisAligned(delta) ?? arrival;
				if (!visited.Add((next, nextArrival)))
				{
					throw new InvalidOperationException(
						$"Tile redirects loop forever starting from {from} moving {direction} (revisited {next})");
				}

				plan.Segments.Add(new PathSegment(current, next, redirect.Value.Kind));
				current = next;
				arrival = nextArrival;
			}

			return plan;
		}

		private static GridVector? ToUnitIfAxisAligned(GridVector delta)
		{
			if (delta.x == 0 && delta.y != 0)
			{
				return new GridVector(0, Math.Sign(delta.y));
			}

			if (delta.y == 0 && delta.x != 0)
			{
				return new GridVector(Math.Sign(delta.x), 0);
			}

			return null;
		}
	}
}
