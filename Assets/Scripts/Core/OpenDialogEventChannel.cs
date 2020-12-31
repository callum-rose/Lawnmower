using System;
using Sirenix.OdinInspector;
using UI.Buttons;
using UI.Dialogs;
using UnityEngine;

namespace Core
{
	[CreateAssetMenu(fileName = nameof(OpenDialogEventChannel),
		menuName = SONames.CoreDir + nameof(OpenDialogEventChannel))]
	public class OpenDialogEventChannel : ScriptableObject
	{
		[SerializeField] private DialogManager dialogManager;

		[TitleGroup("Dialog Info")] [SerializeField]
		private string header;

		[SerializeField] private string body;
		[SerializeField] private ButtonInfo button1;
		[SerializeField] private bool useButton2;

		[SerializeField, ShowIf(nameof(useButton2))]
		private ButtonInfo button2;

		public void Open()
		{
			if (useButton2)
			{
				dialogManager.Show(header, body, button1, button2);
			}
			else
			{
				dialogManager.Show(header, body, button1);
			}
		}
	}
}