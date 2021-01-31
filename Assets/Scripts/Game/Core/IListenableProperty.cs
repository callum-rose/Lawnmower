using System;
using Game.UndoSystem;

namespace Game.Core
{
	public interface IListenableProperty<out T>
	{
		event UndoableAction<T> ValueChanged;
		T Value { get; }
	}
}