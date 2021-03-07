using Game.UndoSystem;

namespace Game.Core
{
	public interface IListenableProperty<out T>
	{
		event UndoableAction<T, T> ValueChangedFromTo;
		
		T Value { get; }
	}
}