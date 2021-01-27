using System;
using Core.EventChannels;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Buttons
{
	[Serializable]
	public struct ButtonInfo
	{
		[SerializeField] private string message;
		[SerializeField] private IconType icon;
		[SerializeField] private Color? colour;

		[SerializeField, InfoBox("This button will just close the dialog", InfoMessageType.Info, nameof(ChannelNull))]
		private BaseEventChannel channel;

		private bool ChannelNull => channel == null;
		
		private Action _action;

		public ButtonInfo(string message = null, IconType icon = IconType.None, Color? colour = null, Action action = null) : this()
		{
			this.message = message;
			this.icon = icon;
			this.colour = colour;
			_action = action;
		}

		public string Message => message;
		public IconType Icon => icon;
		public Color Colour => colour ?? Color.white;
		public Action Action => InvokeAll;

		public void AppendAction(Action action)
		{
			Action tempAction = _action;
			_action = () =>
			{
				tempAction?.Invoke();
				action.Invoke();
			};
		}
		
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