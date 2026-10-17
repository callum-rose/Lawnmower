using System;
using System.Collections.Generic;
using Game.Core;
using Game.UndoSystem;
using Lawnmower.Model;

namespace Game.Mowers
{
	/// <summary>
	/// The mower position the mower prefab's components animate from. Fed by <see cref="MowerManager"/>
	/// from presenter calls, not bound to the simulation.
	/// </summary>
	internal sealed class MowerMotion : IMowerPosition, IListenableProperty<GridVector>
	{
		public event UndoableAction<GridVector, GridVector> ValueChangedFromTo;
		public event Action<IReadOnlyList<PathSegment>> Bounced;

		public IListenableProperty<GridVector> CurrentPosition => this;

		public GridVector Value { get; private set; }

		public PathSegment? CurrentSegment { get; private set; }

		/// <summary>Inverted moves snap (start placement, undo); forward moves animate.</summary>
		public void Publish(GridVector from, GridVector to, Xor inverted)
		{
			Publish(from, to, inverted, null);
		}

		/// <summary>One forward leg of a move.</summary>
		public void Publish(PathSegment segment)
		{
			Publish(segment.From, segment.To, false, segment);
		}

		public void PublishBounce(IReadOnlyList<PathSegment> attempted)
		{
			Bounced?.Invoke(attempted);
		}

		private void Publish(GridVector from, GridVector to, Xor inverted, PathSegment? segment)
		{
			Value = to;
			CurrentSegment = segment;
			ValueChangedFromTo?.Invoke(from, to, inverted);
			CurrentSegment = null;
		}
	}
}
