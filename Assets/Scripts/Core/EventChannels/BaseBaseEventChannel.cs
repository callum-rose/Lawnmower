using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Core.EventChannels
{
	public abstract class BaseBaseEventChannel : ScriptableObject
	{
		protected abstract bool ShouldBeSolo { get; }

		private static readonly List<Type> initialisedTypes = new List<Type>();

#if UNITY_EDITOR
		protected virtual void Awake()
		{
			if (!ShouldBeSolo)
			{
				return;
			}

			Type type = GetType();

			if (initialisedTypes.Contains(type))
			{
				IEnumerable<string> paths = AssetDatabase.FindAssets("t:" + type.Name)
					.Select(AssetDatabase.GUIDToAssetPath);
				int sum = paths.Count();

				StringBuilder sb =
					new StringBuilder($"Event Channel {type} should be solo but {sum} has / have been found:\n");
				foreach (string path in paths)
				{
					sb.AppendLine(path);
				}

				//Debug.LogError(sb.ToString());
			}

			initialisedTypes.Add(type);
		}
#endif
	}

	public abstract class BaseEventChannel : BaseBaseEventChannel, IEventChannelTransmitter, IEventChannelListener
	{
		public event Action EventRaised;

		public virtual void Raise()
		{
			EventRaised?.Invoke();
		}
	}

	public interface IEventChannelTransmitter
	{
		void Raise();
	}

	public interface IEventChannelListener
	{
		event Action EventRaised;
	}

	public abstract class BaseEventChannel<T> : BaseBaseEventChannel, IEventChannelTransmitter<T>,
		IEventChannelListener<T>
	{
		public event Action<T> EventRaised;

		public virtual void Raise(T arg)
		{
			EventRaised?.Invoke(arg);
		}
	}

	public interface IEventChannelTransmitter<in T>
	{
		void Raise(T arg);
	}

	public interface IEventChannelListener<out T>
	{
		event Action<T> EventRaised;
	}

	public abstract class BaseReturnEventChannel<T, TReturn> : BaseBaseEventChannel,
		IReturnEventChannelTransmitter<T, TReturn>, IReturnEventChannelListener<T, TReturn>
	{
		public event Func<T, TReturn> EventRaised;

		public virtual TReturn Raise(T arg)
		{
			return EventRaised == null ? default : EventRaised.Invoke(arg);
		}
	}

	public interface IReturnEventChannelTransmitter<in T, out TReturn>
	{
		TReturn Raise(T arg);
	}

	public interface IReturnEventChannelListener<out T, in TReturn>
	{
		event Func<T, TReturn> EventRaised;
	}

	public abstract class BaseEventChannel<T0, T1> : BaseBaseEventChannel, IEventChannelTransmitter<T0, T1>,
		IEventChannelListener<T0, T1>
	{
		public event Action<T0, T1> EventRaised;

		public virtual void Raise(T0 arg0, T1 arg1)
		{
			EventRaised?.Invoke(arg0, arg1);
		}
	}

	public interface IEventChannelTransmitter<in T0, in T1>
	{
		void Raise(T0 arg0, T1 arg1);
	}

	public interface IEventChannelListener<out T0, out T1>
	{
		event Action<T0, T1> EventRaised;
	}

	public abstract class BaseReturnEventChannel<T0, T1, TReturn> : BaseBaseEventChannel,
		IReturnEventChannelTransmitter<T0, T1, TReturn>, IReturnEventChannelListener<T0, T1, TReturn>
	{
		public event Func<T0, T1, TReturn> EventRaised;

		public virtual TReturn Raise(T0 arg0, T1 arg1)
		{
			return EventRaised == null ? default : EventRaised.Invoke(arg0, arg1);
		}
	}

	public interface IReturnEventChannelTransmitter<in T0, in T1, out TReturn>
	{
		TReturn Raise(T0 arg0, T1 arg1);
	}

	public interface IReturnEventChannelListener<out T0, out T1, in TReturn>
	{
		event Func<T0, T1, TReturn> EventRaised;
	}
}