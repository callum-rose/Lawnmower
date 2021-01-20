using Core;
using Core.EventChannels;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Dialogs
{
	[CreateAssetMenu(fileName = nameof(PresetOpenDialogEventChannel), menuName = SONames.GameDir + nameof(PresetOpenDialogEventChannel))]
	public class PresetOpenDialogEventChannel : ScriptableObject
	{
		[Required, SerializeField] private OpenDialogEventChannel openDialogEventChannel;
		[SerializeField, InlineProperty, HideLabel] private DialogInfo dialogInfo;
		
		public void Raise()
		{
			openDialogEventChannel.Raise(dialogInfo);
		}
	}
}