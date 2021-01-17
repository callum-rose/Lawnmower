using UI.Dialogs;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(OpenDialogEventChannel),
		menuName = SONames.CoreDir + nameof(OpenDialogEventChannel))]
	public class OpenDialogEventChannel : BaseReturnEventChannel<DialogInfo, int>
	{
		protected override bool ShouldBeSolo => true;
	}
}