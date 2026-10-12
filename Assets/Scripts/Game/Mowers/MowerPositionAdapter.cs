using Game.Core;
using Game.UndoSystem;
using Lawnmower.Model;

namespace Game.Mowers
{
	/// <summary>
	/// Exposes the simulation's mower position to the mower views, and publishes the start
	/// placement those views need to show the mower.
	/// </summary>
	internal sealed class MowerPositionAdapter : IMowerPosition
	{
		public IListenableProperty<GridVector> CurrentPosition => _currentPosition;

		private readonly ForwardingProperty _currentPosition;

		public MowerPositionAdapter(LevelSimulation simulation)
		{
			_currentPosition = new ForwardingProperty(simulation.MowerPosition);
		}

		/// <summary>
		/// Raises (Zero -> start, inverted) so views snap the mower to its start tile without
		/// animating, as the old SetInitialPosition did.
		/// </summary>
		public void PublishInitial()
		{
			_currentPosition.Publish(GridVector.Zero, _currentPosition.Value, true);
		}

		public void Dispose()
		{
			_currentPosition.Dispose();
		}

		private sealed class ForwardingProperty : IListenableProperty<GridVector>
		{
			public event UndoableAction<GridVector, GridVector> ValueChangedFromTo;

			public GridVector Value => _source.Value;

			private readonly IListenableProperty<GridVector> _source;

			public ForwardingProperty(IListenableProperty<GridVector> source)
			{
				_source = source;
				_source.ValueChangedFromTo += Publish;
			}

			public void Publish(GridVector from, GridVector to, Xor inverted)
			{
				ValueChangedFromTo?.Invoke(from, to, inverted);
			}

			public void Dispose()
			{
				_source.ValueChangedFromTo -= Publish;
			}
		}
	}
}
