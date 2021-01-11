using System;

namespace Game.Tiles
{
	internal interface IListenableProperty<out T>
	{
		event Action<T> ValueChanged;
		T Value { get; }
	}
}