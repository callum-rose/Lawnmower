using System;
using Sirenix.OdinInspector;
using UI.Buttons;
using UnityEngine;

namespace UI.Dialogs
{
	[Serializable]
	public class DialogInfo
	{
		[TitleGroup("Dialog Info")] [SerializeField]
		public string header;

		[SerializeField] private string body;
		[SerializeField] private ButtonInfo button1;
		[SerializeField] private ButtonInfo button2;

		public DialogInfo(string header, string body, ButtonInfo button1)
		{
			this.header = header;
			this.body = body;
			this.button1 = button1;
		}

		public DialogInfo(string header, string body, ButtonInfo button1, ButtonInfo button2)
		{
			this.header = header;
			this.body = body;
			this.button1 = button1;
			this.button2 = button2;
		}

		public string Body => body;
		public ButtonInfo Button1 => button1;
		public ButtonInfo Button2 => button2;
		
		[Button]
		private void ClearButton2Info()
		{
			button2 = default;
		}
	}
}