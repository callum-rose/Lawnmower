using System;
using UnityEngine;

namespace Core
{
	public abstract class BaseEventChannel : ScriptableObject
	{
		public event Action EventRaised;

		public void Raise()
		{
			EventRaised?.Invoke();
		}
	}

	
	public abstract class BaseEventChannel<T> : ScriptableObject
	{
		public event Action<T> EventRaised;
		
		public void Raise(T arg)
		{
			EventRaised?.Invoke(arg);
		}
	}
	
	public abstract class BaseEventChannel<T0, T1> : ScriptableObject
	{
		public event Action<T0, T1> EventRaised;
		
		public void Raise(T0 arg0, T1 arg1)
		{
			EventRaised?.Invoke(arg0, arg1);
		}
	}

	// public abstract class BaseBaseEventChannel : ScriptableObject
	// {
	// 	// protected abstract Delegate EventDelegate { get; }
	// 	//
	// 	// private string EventSubscribersStr
	// 	// {
	// 	// 	get
	// 	// 	{
	// 	// 		if (EventDelegate == null)
	// 	// 		{
	// 	// 			return "Event has no listeners";
	// 	// 		}
	// 	//
	// 	// 		Delegate[] list = EventDelegate.GetInvocationList();
	// 	// 		StringBuilder sb = new StringBuilder("Listeners:\n");
	// 	// 		for (int index = 0; index < list.Length; index++)
	// 	// 		{
	// 	// 			Delegate d = list[index];
	// 	// 			sb.Append(d.Method.DeclaringType + "." + d.Method.GetFullName());
	// 	//
	// 	// 			if (typeof(Object).IsAssignableFrom(d.Method.DeclaringType))
	// 	// 			{
	// 	// 				Object obj = d.Target as Object;
	// 	// 				sb.Append(" \"" + obj.name + "\"");
	// 	// 			}
	// 	//
	// 	// 			if (index != list.Length - 1)
	// 	// 			{
	// 	// 				sb.Append("\n");
	// 	// 			}
	// 	// 		}
	// 	//
	// 	// 		return sb.ToString();
	// 	// 	}
	// 	// }
	// }
}