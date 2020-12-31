using System;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core
{
	public abstract class BaseEventChannel : ScriptableObject
	{
		protected abstract Delegate EventDelegate { get; }

		private string EventSubscribersStr
		{
			get
			{
				if (EventDelegate == null)
				{
					return "Event has no listeners";
				}

				Delegate[] list = EventDelegate.GetInvocationList();
				StringBuilder sb = new StringBuilder("Listeners:\n");
				for (int index = 0; index < list.Length; index++)
				{
					Delegate d = list[index];
					sb.Append(d.Method.DeclaringType + "." + d.Method.GetFullName());

					if (typeof(Object).IsAssignableFrom(d.Method.DeclaringType))
					{
						Object obj = d.Target as Object;
						sb.Append(" \"" + obj.name + "\"");
					}

					if (index != list.Length - 1)
					{
						sb.Append("\n");
					}
				}

				return sb.ToString();
			}
		}

		[InfoBox("$" + nameof(EventSubscribersStr))]
		[Button("Test Raise Event")]
		public void RaiseEvent()
		{
			EventDelegate?.DynamicInvoke();
		}
	}
}