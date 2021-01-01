using System;
using Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Buttons
{
	[Serializable]
	public struct ButtonInfo
	{
		[SerializeField] private string message;
		[SerializeField] private IconType icon;

		[SerializeField, InfoBox("This button will just close the dialog", InfoMessageType.Info, nameof(ChannelNull))]
		private BaseEventChannel channel;

		private bool ChannelNull => channel == null;
		
		private Action _action;

		public ButtonInfo(string message = null, IconType icon = IconType.None, Action action = null) : this()
		{
			this.message = message;
			this.icon = icon;
			_action = action;
		}

		public string Message => message;
		public IconType Icon => icon;
		public Action Action => InvokeAll;

		private void InvokeAll()
		{
			_action?.Invoke();
			
			if (channel != null)
			{
				channel.Raise();
			}
		}
	}
}