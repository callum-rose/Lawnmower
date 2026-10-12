using System;
using Game.UndoSystem;

namespace Game.Core
{
	public sealed class EventProperty<T> : IListenableProperty<T>
	{
		public event UndoableAction<T, T> ValueChangedFromTo;

		public T Value => InternalGet();

		public T RawValue => _value;

		private readonly Func<T, T> _getter;

		private T _value;

		public EventProperty(Func<T, T> getter = null)
		{
			_getter = getter;
		}

		public EventProperty(T initialValue, Func<T, T> getter = null) : this(getter)
		{
			_value = initialValue;
		}

		/// <summary>Only the model mutates state; views listen through <see cref="IListenableProperty{T}"/>.</summary>
		internal void SetValue(T value, Xor isInverted)
		{
			T prevValue = InternalGet();

			_value = value;

			ValueChangedFromTo?.Invoke(prevValue, InternalGet(), isInverted);
		}

		/// <summary>Sets the value without raising <see cref="ValueChangedFromTo"/>.</summary>
		internal void SetValueSilently(T value)
		{
			_value = value;
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
