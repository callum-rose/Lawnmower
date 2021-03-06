using System;
using Game.UndoSystem;
using Newtonsoft.Json;

namespace Game.Core
{
	[Serializable]
	public sealed class EventProperty<T> : IListenableProperty<T>
	{
		public event UndoableAction<T, T> ValueChangedFromTo;

		public T Value => InternalGet();

		public T RawValue => _value;

		[JsonIgnore]
		private readonly Func<T, T> _getter;
		
		[JsonRequired]
		private T _value;

		public EventProperty(Func<T, T> getter = null)
		{
			_getter = getter;
		}

		public void SetValue(T value, Xor isInverted)
		{
			T prevValue = InternalGet();
			
			_value = value;
			
			ValueChangedFromTo?.Invoke(prevValue, InternalGet(), isInverted);
		}
		
		private T InternalGet()
		{
			return _getter != null ? _getter(_value) : _value;
		}

		public static implicit operator T(EventProperty<T> property)
		{
			return property.Value;
		}
	}
}