using Game.Core;
using Game.UndoSystem;

namespace Game.Mowers
{
	public class MowerMover : IMowerPosition
	{
		public IListenableProperty<GridVector> CurrentPosition => _internalPosition;

		private readonly EventProperty<GridVector> _internalPosition;

		public MowerMover()
		{
			_internalPosition = new EventProperty<GridVector>();
		}

		public void Move(GridVector toPosition, Xor isInverted)
		{
			_internalPosition.SetValue(toPosition, isInverted);
		}

		public void Bump(GridVector toPosition)
		{
		}
	}
}