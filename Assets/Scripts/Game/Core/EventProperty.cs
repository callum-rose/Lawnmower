using System;
using Newtonsoft.Json;

namespace Game.Core
{
	[Serializable]
	public sealed class EventProperty<T> : IListenableProperty<T>
	{
		public event Action<T> ValueChanged;

		public T Value
		{
			get => InternalGet();
			set => InternalSet(value);
		}
		
		[JsonIgnore]
		private readonly Func<T, T> _getter, _setter;
		
		[JsonRequired]
		private T _value;

		public EventProperty(Func<T, T> getter = null, Func<T, T> setter = null)
		{
			_getter = getter;
			_setter = setter;
		}
		
		private T InternalGet()
		{
			return _getter != null ? _getter(_value) : _value;
		}
        
		private void InternalSet(T value)
		{
			_value = _setter != null ? _setter(value) : value;
			ValueChanged?.Invoke(InternalGet());
		}
		
		public static implicit operator T(EventProperty<T> property)
		{
			return property.Value;
		}
	}
}