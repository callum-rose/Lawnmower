using Core;
using Core.EventChannels;
using UnityEngine;

namespace UI.Dialogs
{
	[CreateAssetMenu(fileName = nameof(PresetOpenDialogEventChannel), menuName = SoNames.GameDir + nameof(PresetOpenDialogEventChannel))]
	public class PresetOpenDialogEventChannel : ScriptableObject
	{
		[SerializeField] private OpenDialogEventChannel openDialogEventChannel;
		[SerializeField] private DialogInfo dialogInfo;
		
		public void Raise()
		{
			openDialogEventChannel.Raise(dialogInfo);
		}
	}
}