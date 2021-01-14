using System;

namespace Game.Core
{
	public interface IListenableProperty<out T>
	{
		event Action<T> ValueChanged;
		T Value { get; }
	}
}