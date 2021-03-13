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
		protected virtual bool ShouldBeSolo => false;

		protected virtual bool PushLastDataOnSubscribe => false;

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

		protected sealed override bool PushLastDataOnSubscribe => false;

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
		public event Action<T> EventRaised
		{
			add
			{
				if (PushLastDataOnSubscribe && _lastArgSet)
				{
					value.Invoke(_lastArg);
				}
				
				EventRaisedInternal += value;
			}

			remove => EventRaisedInternal -= value;
		}

		private event Action<T> EventRaisedInternal;

		private T _lastArg;
		private bool _lastArgSet = false;

		public virtual void Raise(T arg)
		{
			EventRaisedInternal?.Invoke(arg);
			
			_lastArg = arg;
			_lastArgSet = true;
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
		public event Func<T, TReturn> EventRaised
		{
			add
			{
				if (EventRaisedInternal != null && EventRaisedInternal.GetInvocationList().Length > 0)
				{
					Debug.LogError($"There can only be one listener subscribed to an instance of {nameof(BaseReturnEventChannel<T, TReturn>)}");
					return;
				}

				EventRaisedInternal += value;
			}

			remove => EventRaisedInternal -= value;
		}

		protected sealed override bool PushLastDataOnSubscribe => false;

		private event Func<T, TReturn> EventRaisedInternal;
		
		public virtual TReturn Raise(T arg)
		{
			return EventRaisedInternal == null ? default : EventRaisedInternal.Invoke(arg);
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
		public event Action<T0, T1> EventRaised
		{
			add
			{
				if (PushLastDataOnSubscribe && _lastArgSet)
				{
					value.Invoke(_lastArg0, _lastArg1);
				}
				
				EventRaisedInternal += value;
			}

			remove => EventRaisedInternal -= value;
		}

		private event Action<T0, T1> EventRaisedInternal;

		private T0 _lastArg0;
		private T1 _lastArg1;
		private bool _lastArgSet = false;

		public virtual void Raise(T0 arg0, T1 arg1)
		{
			EventRaisedInternal?.Invoke(arg0, arg1);

			_lastArg0 = arg0;
			_lastArg1 = arg1;

			_lastArgSet = true;
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
		public event Func<T0, T1, TReturn> EventRaised
		{
			add
			{
				if (EventRaisedInternal != null && EventRaisedInternal.GetInvocationList().Length > 0)
				{
					Debug.LogError($"There can only be one listener subscribed to an instance of {nameof(BaseReturnEventChannel<T0, T1, TReturn>)}");
					return;
				}

				EventRaisedInternal += value;
			}

			remove => EventRaisedInternal -= value;
		}

		protected sealed override bool PushLastDataOnSubscribe => false;

		private event Func<T0, T1, TReturn> EventRaisedInternal;

		public virtual TReturn Raise(T0 arg0, T1 arg1)
		{
			return EventRaisedInternal == null ? default : EventRaisedInternal.Invoke(arg0, arg1);
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