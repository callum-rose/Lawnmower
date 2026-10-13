using Game.Core;
using Game.UndoSystem;

namespace Game.Mowers
{
	/// <summary>
	/// The mower position the mower prefab's components animate from. Fed by <see cref="MowerManager"/>
	/// from presenter calls, not bound to the simulation.
	/// </summary>
	internal sealed class MowerMotion : IMowerPosition, IListenableProperty<GridVector>
	{
		public event UndoableAction<GridVector, GridVector> ValueChangedFromTo;

		public IListenableProperty<GridVector> CurrentPosition => this;

		public GridVector Value { get; private set; }

		/// <summary>Inverted moves snap (start placement, undo); forward moves animate.</summary>
		public void Publish(GridVector from, GridVector to, Xor inverted)
		{
			Value = to;
			ValueChangedFromTo?.Invoke(from, to, inverted);
		}
	}
}
