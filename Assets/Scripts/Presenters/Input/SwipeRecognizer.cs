using System;
using Game.Core;

namespace Lawnmower.Presenters
{
	/// <summary>
	/// Turns one touch into at most one swipe. The swipe fires as soon as the touch has moved
	/// <see cref="MinDistance"/> from where it started, provided the path so far was straight enough
	/// (displacement at least <see cref="MinStraightness"/> of the distance travelled). Positions are in screen pixels.
	/// </summary>
	public sealed class SwipeRecognizer
	{
		public float MinDistance { get; }
		public float MinStraightness { get; }

		/// <summary>True between <see cref="Begin"/> and the swipe, rejection or <see cref="End"/>.</summary>
		public bool IsTracking { get; private set; }

		private float _startX, _startY, _lastX, _lastY, _pathLength;

		public SwipeRecognizer(float minDistance, float minStraightness = 0.8f)
		{
			if (minDistance <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(minDistance), minDistance, "must be positive");
			}

			if (minStraightness < 0 || minStraightness > 1)
			{
				throw new ArgumentOutOfRangeException(nameof(minStraightness), minStraightness, "must be 0-1");
			}

			MinDistance = minDistance;
			MinStraightness = minStraightness;
		}

		/// <summary>A touch went down. Restarts tracking.</summary>
		public void Begin(float x, float y)
		{
			_startX = _lastX = x;
			_startY = _lastY = y;
			_pathLength = 0;
			IsTracking = true;
		}

		/// <summary>
		/// The touch moved. Returns true exactly once per touch, when it becomes a swipe, with the start and
		/// the current position; tracking stops until the next <see cref="Begin"/>.
		/// </summary>
		public bool Move(float x, float y, out Swipe swipe)
		{
			swipe = default;

			if (!IsTracking)
			{
				return false;
			}

			_pathLength += Length(x - _lastX, y - _lastY);
			_lastX = x;
			_lastY = y;

			float displacement = Length(x - _startX, y - _startY);
			if (displacement < MinDistance)
			{
				return false;
			}

			// One decision per touch: a wiggly path never becomes a swipe later in the same touch.
			IsTracking = false;

			if (displacement < _pathLength * MinStraightness)
			{
				return false;
			}

			swipe = new Swipe(_startX, _startY, x, y);
			return true;
		}

		/// <summary>The touch lifted or was cancelled.</summary>
		public void End() => IsTracking = false;

		/// <summary>The cardinal direction along the dominant axis of (dx, dy); Zero for no movement. Ties go to the y axis.</summary>
		public static GridVector ToCardinal(float dx, float dy)
		{
			if (dx == 0 && dy == 0)
			{
				return GridVector.Zero;
			}

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				return dx > 0 ? GridVector.Right : GridVector.Left;
			}

			return dy > 0 ? GridVector.Up : GridVector.Down;
		}

		private static float Length(float dx, float dy) => (float)Math.Sqrt(dx * dx + dy * dy);
	}

	/// <summary>A recognised swipe, from its start to where it was recognised (screen pixels).</summary>
	public readonly struct Swipe
	{
		public readonly float StartX, StartY, EndX, EndY;

		public Swipe(float startX, float startY, float endX, float endY)
		{
			StartX = startX;
			StartY = startY;
			EndX = endX;
			EndY = endY;
		}

		public float DeltaX => EndX - StartX;
		public float DeltaY => EndY - StartY;
	}
}
