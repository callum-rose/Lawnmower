using Game.Core;

namespace Game.Mowers
{
	public class MowerMoverr : IMowerPosition
	{
		public IListenableProperty<GridVector> CurrentPosition => _internalPosition;

		private readonly EventProperty<GridVector> _internalPosition;

		public MowerMoverr()
		{
			_internalPosition = new EventProperty<GridVector>();
		}

		public void Move(GridVector toPosition)
		{
			_internalPosition.Value = toPosition;
		}

		public void Bump(GridVector toPosition)
		{
		}
	}
}