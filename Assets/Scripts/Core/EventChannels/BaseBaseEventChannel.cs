using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Core.EventChannels
{
	public abstract class BaseBaseEventChannel : ScriptableObject
	{
		protected abstract bool ShouldBeSolo { get; }

		private static readonly List<Type> initialisedTypes = new List<Type>();
		
		private void Awake()
		{
			if (!ShouldBeSolo)
			{
				return;
			}
			
			Type type = GetType();
			
			if (initialisedTypes.Contains(type))
			{
				IEnumerable<string> paths = AssetDatabase.FindAssets("t:" + type.Name).Select(AssetDatabase.GUIDToAssetPath);
				int sum = paths.Count();
				
				StringBuilder sb = new StringBuilder($"Event Channel {type} should be solo but {sum} has / have been found:\n");
				foreach (string path in paths)
				{
					sb.AppendLine(path);
				}
				
				Debug.LogError(sb.ToString());
			}
			
			initialisedTypes.Add(type);
		}
	}
	
	public abstract class BaseEventChannel : BaseBaseEventChannel
	{
		public event Action EventRaised;

		public void Raise()
		{
			EventRaised?.Invoke();
		}
	}

	public abstract class BaseEventChannel<T> : BaseBaseEventChannel
	{
		public event Action<T> EventRaised;

		public void Raise(T arg)
		{
			EventRaised?.Invoke(arg);
		}
	}

	public abstract class BaseReturnEventChannel<T, TReturn> : BaseBaseEventChannel
	{
		public event Func<T, TReturn> EventRaised;

		public TReturn Raise(T arg)
		{
			return EventRaised == null ? default : EventRaised.Invoke(arg);
		}
	}

	public abstract class BaseEventChannel<T0, T1> : BaseBaseEventChannel
	{
		public event Action<T0, T1> EventRaised;

		public void Raise(T0 arg0, T1 arg1)
		{
			EventRaised?.Invoke(arg0, arg1);
		}
	}
	
	public abstract class BaseReturnEventChannel<T0, T1, TReturn> : BaseBaseEventChannel
	{
		public event Func<T0, T1, TReturn> EventRaised;

		public TReturn Raise(T0 arg0, T1 arg1)
		{
			return EventRaised == null ? default : EventRaised.Invoke(arg0, arg1);
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